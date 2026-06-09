using CalendarCountdown.Solution.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
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

// Configure OAuth 2.0
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        options.SaveTokens = true; // Required to pass the token to the Calendar API
        options.Scope.Add("https://www.googleapis.com/auth/calendar.readonly");
    });

builder.Services.AddHttpContextAccessor();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
