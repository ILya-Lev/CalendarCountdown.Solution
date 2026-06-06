using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace CalendarCountdown.Solution.Services;

public class GoogleCalendarService : ICalendarService
{
    private readonly CalendarService _calendarService;
    private readonly string[] _calendarIds;

    public GoogleCalendarService(IConfiguration config)
    {
        _calendarIds = config.GetSection("GoogleCalendar:CalendarIds").Get<string[]>() ?? [];

        var authKeys = config.GetSection("GoogleCalendar:ServiceAccount")
            .GetChildren()
            .ToDictionary(x => x.Key, x => x.Value);
        var jsonCredentials = JsonSerializer.Serialize(authKeys);

        var credential = CredentialFactory
            .FromJson<ServiceAccountCredential>(jsonCredentials)
            .ToGoogleCredential()
            .CreateScoped(CalendarService.Scope.CalendarReadonly);

        _calendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CalendarCountdown"
        });
    }

    public async Task<IList<(string CalendarId, Event ev)>> GetUpcomingEventsAsync(int maxResults = 10)
    {
        var tasks = _calendarIds.Select(cid => GetUpcomingEventsPerCalendar(cid, maxResults).ContinueWith(t => (t.Result, cid)));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r.Result.Select(ev => (r.cid, ev)))
            .OrderBy(r => r.ev.Start.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(r.ev.Start.Date))
            .ThenBy(r => r.ev.End.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(r.ev.End.Date)).ToArray();
    }


    private async Task<IList<Event>> GetUpcomingEventsPerCalendar(string calendarId, int maxResults = 10)
    {
        var request = _calendarService.Events.List(calendarId);
        request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow;
        request.ShowDeleted = false;
        request.SingleEvents = true;
        request.MaxResults = maxResults;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;

        var events = await request.ExecuteAsync();
        return events.Items;
    }

    public async Task<Event?> GetEventAsync(string eventId)
    {
        foreach (var calendarId in _calendarIds)
        {
            try
            {
                var request = _calendarService.Events.Get(calendarId, eventId);
                return await request.ExecuteAsync();
            }
            catch { }
        }

        throw new InvalidOperationException(
            $"cannot find event by id {eventId} in any of the calendars {string.Join(", ", _calendarIds)}");
    }
}