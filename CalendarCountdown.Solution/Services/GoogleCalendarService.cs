using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.AspNetCore.Authentication;

namespace CalendarCountdown.Solution.Services;

public class GoogleCalendarService : ICalendarService
{
    private readonly Lazy<Task<CalendarService>> _calendarService;
    private readonly string[] _calendarIds;

    public GoogleCalendarService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
    {
        _calendarIds = config.GetSection("GoogleCalendar:CalendarIds").Get<string[]>() ?? [];

        //var authKeys = config.GetSection("GoogleCalendar:ServiceAccount")
        //    .GetChildren()
        //    .ToDictionary(x => x.Key, x => x.Value);
        //var jsonCredentials = JsonSerializer.Serialize(authKeys);

        //var credential = CredentialFactory
        //    .FromJson<ServiceAccountCredential>(jsonCredentials)
        //    .ToGoogleCredential()
        //    .CreateScoped(CalendarService.Scope.CalendarEventsOwnedReadonly);

        //_calendarService = new CalendarService(new BaseClientService.Initializer
        //{
        //    HttpClientInitializer = credential,
        //    ApplicationName = "CalendarCountdown"
        //});

        _calendarService = new Lazy<Task<CalendarService>>(async () => await CreateCalendarService(httpContextAccessor));
    }

    private async Task<CalendarService> CreateCalendarService(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext
                      ?? throw new InvalidOperationException("No HTTP context available.");

        // Extract the token saved during the Google sign-in flow
        var accessToken = await context.GetTokenAsync("access_token")
                          ?? throw new UnauthorizedAccessException("Google access token is missing.");

        var credential = GoogleCredential.FromAccessToken(accessToken);

        return new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CalendarCountdown"
        });
    }

    public async Task<IList<(string CalendarId, Event ev)>> GetUpcomingEventsAsync(int maxResults = 10)
    {
        var tasks = _calendarIds.Select(async cid =>
        {
            try
            {
                var events = await GetUpcomingEventsPerCalendar(cid, maxResults);
                return (CalendarId: cid, Events: events ?? [], Success: true);
            }
            catch (Exception)
            {
                return (CalendarId: cid, Events: [], Success: false);
            }
        });

        var results = await Task.WhenAll(tasks);

        return results
            .Where(r => r.Success && r.Events.Any())
            .SelectMany(r => r.Events.Select(ev => (r.CalendarId, ev)))
            .OrderBy(r => r.ev.Start.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(r.ev.Start.Date))
            .ThenBy(r => r.ev.End.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(r.ev.End.Date))
            .ToArray();
    }


    private async Task<IList<Event>> GetUpcomingEventsPerCalendar(string calendarId, int maxResults = 10)
    {
        var request = (await _calendarService.Value).Events.List(calendarId);
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
                var request = (await _calendarService.Value).Events.Get(calendarId, eventId);
                return await request.ExecuteAsync();
            }
            catch { }
        }

        throw new InvalidOperationException(
            $"cannot find event by id {eventId} in any of the calendars {string.Join(", ", _calendarIds)}");
    }
}