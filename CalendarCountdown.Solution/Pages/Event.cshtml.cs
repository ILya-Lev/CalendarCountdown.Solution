using CalendarCountdown.Solution.Services;
using Google;
using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalendarCountdown.Solution.Pages;

public class EventModel(ICalendarService calendarService, ILocationReferenceProvider locationReferenceProvider)
    : PageModel
{
    public string EventName { get; private set; } = string.Empty;
    public string? StartTimeFormatted { get; private set; }
    public string? EndTimeFormatted { get; private set; }
    public long? TargetTimestamp { get; private set; }
    public string? Description { get; private set; }
    public string? HtmlLink { get; private set; }
    public string? DisplayLocationText { get; private set; }
    public string? MapIframeSrc { get; private set; }
    public string? MapExternalUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync([FromQuery] string calendarId, [FromQuery] string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToPage("/Index");

        Event? ev = null;
        try
        {
            ev = await calendarService.GetEventAsync(id);
            if (ev is null) return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Challenge(); //token is missing
        }
        catch (GoogleApiException exc) when (exc.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return Challenge(); //token is present, but expired
        }

        EventName = ev.Summary;
        Description = ev.Description;
        HtmlLink = ev.HtmlLink;
        var locationData = await locationReferenceProvider.ParseLocationData(ev.Location, ev.Description);
        if (locationData is not null)
        {
            DisplayLocationText = locationData.Value.DisplayLocationText;
            MapExternalUrl = locationData.Value.MapExternalUrl;
            MapIframeSrc = locationData.Value.MapIframeSrc;
        }

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