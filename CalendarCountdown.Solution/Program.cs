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
builder.Services.AddWindowsService(lifetimeOptions => { lifetimeOptions.ServiceName = "CalendarCountdown"; });

builder.Services.AddRazorPages();
builder.Services.AddScoped<ICalendarService, GoogleCalendarService>();
builder.Services.AddHttpClient<ILocationReferenceProvider, LocationReferenceProvider>();

// Configure OAuth 2.0
builder.Services.AddAuthentication(configureOptions =>
    {
        configureOptions.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        configureOptions.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddGoogle(googleOptions =>
    {
        var clientId = builder.Configuration["Authentication:Google:ClientId"];
        var clientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException("CRITICAL: Google OAuth credentials are missing. Check your appsettings.Secrets.json file.");
        }

        googleOptions.ClientId = clientId;
        googleOptions.ClientSecret = clientSecret;
        googleOptions.SaveTokens = true; // Required to pass the token to the Calendar API
        googleOptions.Scope.Add("https://www.googleapis.com/auth/calendar.readonly");
        googleOptions.AccessType = "offline";// Request a refresh token for background auto-renewal
        googleOptions.AdditionalAuthorizationParameters.Add("prompt", "consent");// Force consent screen to guarantee Google returns the refresh token
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
