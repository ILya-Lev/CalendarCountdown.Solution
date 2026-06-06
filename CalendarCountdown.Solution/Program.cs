using CalendarCountdown.Solution.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.SemanticKernel;

// Ensure the app finds its files when executed by the Windows Service Control Manager
var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService()
        ? AppContext.BaseDirectory
        : null
};

var builder = WebApplication.CreateBuilder(options);

// Explicitly load the copied secrets file into the configuration pipeline
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Register Windows Service lifetime and define the service name
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CalendarCountdown";
});

builder.Services.AddRazorPages();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddHttpClient<ILocationReferenceProvider, LocationReferenceProvider>();

// Register Semantic Kernel to enable importing skills/plugins
builder.Services.AddKernel();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
