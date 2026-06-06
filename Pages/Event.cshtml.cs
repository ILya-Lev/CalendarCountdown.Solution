using CalendarCountdown.Solution.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalendarCountdown.Pages;

public class EventModel(ICalendarService calendarService) : PageModel
{
    public string EventName { get; private set; } = string.Empty;
    public string? StartTimeFormatted { get; private set; }
    public string? EndTimeFormatted { get; private set; }
    public long? TargetTimestamp { get; private set; }
    public string? Description { get; private set; }
    public string? HtmlLink { get; private set; }
    public string? Location { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromQuery] string calendarId, [FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToPage("/Index");

        var ev = await calendarService.GetEventAsync(id);
        if (ev is null) return NotFound();

        EventName = ev.Summary;
        Description = ev.Description;
        HtmlLink = ev.HtmlLink;
        Location = ev.Location;

        var dt = ev.Start.DateTimeDateTimeOffset?.LocalDateTime ?? DateTimeOffset.Parse(ev.Start.Date).LocalDateTime;
        InitializeProperties(dt);
        EndTimeFormatted = (ev.End.DateTimeDateTimeOffset ?? DateTimeOffset.Parse(ev.End.Date)).LocalDateTime.ToString("f");

        return Page();
    }

    private void InitializeProperties(DateTimeOffset dt)
    {
        StartTimeFormatted = dt.ToString("f");
        TargetTimestamp = dt.ToUnixTimeMilliseconds(); // Pass raw milliseconds to JS
    }
}