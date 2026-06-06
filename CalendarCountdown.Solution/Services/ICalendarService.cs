namespace CalendarCountdown.Solution.Services;

using Google.Apis.Calendar.v3.Data;

public interface ICalendarService
{
    Task<IList<(string CalendarId, Event ev)>> GetUpcomingEventsAsync(int maxResults = 10);
    Task<Event?> GetEventAsync(string eventId);
}