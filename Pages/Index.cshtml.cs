using System.Text.Json;
using CalendarCountdown.Solution.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalendarCountdown.Solution.Pages;

public class IndexModel(ICalendarService calendarService) : PageModel
{
    private const long Hour = 60 * 60 * 1000;
    private const long Day24Hours = Hour * 24;
    private const long MinDurationMs = Hour * 6;

    public string EventsJson { get; private set; } = "[]";

    public async Task OnGetAsync()
    {
        // Project into an anonymous type for clean JSON serialization
        var displayEvents = (await calendarService.GetUpcomingEventsAsync()).Select(pair =>
        {
            long startMs = 0;
            long endMs = 0;

            if (pair.ev.Start.DateTimeDateTimeOffset.HasValue)
            {
                startMs = pair.ev.Start.DateTimeDateTimeOffset.Value.ToUnixTimeMilliseconds();
                endMs = pair.ev.End?.DateTimeDateTimeOffset?.ToUnixTimeMilliseconds()
                        ?? startMs + Hour; // Default 1 hr if missing
            }
            else if (!string.IsNullOrEmpty(pair.ev.Start.Date))
            {
                // All-day event fallback
                if (DateTime.TryParse(pair.ev.Start.Date, out var dtStart))
                    startMs = new DateTimeOffset(dtStart).ToUnixTimeMilliseconds();

                if (DateTime.TryParse(pair.ev.End?.Date, out var dtEnd))
                    endMs = new DateTimeOffset(dtEnd).ToUnixTimeMilliseconds();
                else
                    endMs = startMs + Day24Hours; // Default 1 day
            }

            return new
            {
                id = pair.ev.Id,
                calendarId = pair.CalendarId,
                name = string.IsNullOrEmpty(pair.ev.Summary) ? "(No title)" : pair.ev.Summary,
                startMs,
                endMs
            };
        })
        .Where(x => x.startMs > 0 && x.endMs >= x.startMs)
        .OrderBy(x => x.startMs)
        .ToArray();

        // --- ENFORCE MINIMUM VISUAL DURATION ---
        for (int i = 1; i < displayEvents.Length; i++)
        {
            if (displayEvents[i - 1].endMs + MinDurationMs < displayEvents[i].startMs)
                displayEvents[i - 1] = displayEvents[i - 1] with
                {
                    endMs = displayEvents[i].startMs - MinDurationMs / 2
                };
        }

        EventsJson = JsonSerializer.Serialize(displayEvents);
    }
}