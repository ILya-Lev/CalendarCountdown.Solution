using CalendarCountdown.Solution.Services;
using Google.Apis.Calendar.v3.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CalendarCountdown.Solution.Pages;

public class IndexModel(ICalendarService calendarService) : PageModel
{
    public IList<Event> Events { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Events = await calendarService.GetUpcomingEventsAsync();
    }
}