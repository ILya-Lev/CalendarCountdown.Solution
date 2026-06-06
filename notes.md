The steps, configurations, and commands required to publish and install the application as a Windows Service.

### 1. Project Configuration

**`CalendarCountdown.csproj`**
This final version enables **ReadyToRun** (R2R) optimization, single-file deployment, safe trimming, warning suppression, and an automated task to copy your user secrets.

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net11.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UserSecretsId>the id</UserSecretsId>

    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <PublishTrimmed>true</PublishTrimmed>

    <WarningLevel>0</WarningLevel>
    <SuppressTrimAnalysisWarnings>true</SuppressTrimAnalysisWarnings>
  </PropertyGroup>

  <Target Name="CopySecretsToPublishDirectory" AfterTargets="Publish">
    <PropertyGroup>
      <SecretsFile>$([System.Environment]::GetFolderPath(SpecialFolder.ApplicationData))\Microsoft\UserSecrets\$(UserSecretsId)\secrets.json</SecretsFile>
    </PropertyGroup>
    <Copy SourceFiles="$(SecretsFile)" 
          DestinationFiles="$(PublishDir)appsettings.Secrets.json" 
          Condition="Exists('$(SecretsFile)')" />
  </Target>

  <ItemGroup>
    <PackageReference Include="Google.Apis.Auth" Version="1.75.0" />
    <PackageReference Include="Google.Apis.Calendar.v3" Version="1.74.0.4154" />
    <PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="10.0.8" />
  </ItemGroup>

</Project>

```

**`Program.cs` Adjustments**
Ensure the host uses the correct root directory, registers the Windows Service, and explicitly loads the copied secrets file.

```csharp
using Microsoft.Extensions.Hosting.WindowsServices;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};

var builder = WebApplication.CreateBuilder(options);

builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "CalendarCountdown";
});

// ... rest of your service registrations

```

---

### 2. Publishing the Application

Publish a self-contained and R2R-optimized app with min console noise, still showing elapsed time:

```bash
dotnet publish -c Release -o C:\deployments\CalendarCountdown -r win-x64 --self-contained true -p:WarningLevel=0 -tl

```

---

### 3. Windows Service Installation

Open an **Administrator PowerShell** prompt and execute these commands:

**Install:**

```powershell
sc.exe create "CalendarCountdown" binpath= "C:\path\CalendarCountdown\CalendarCountdown.exe" start= auto

```

*(Note: The spaces after `binpath=` and `start=` are mandatory).*

**Start:**

```powershell
sc.exe start "CalendarCountdown"

```

**Stop / Delete (if you need to cleanly reinstall):**

```powershell
sc.exe stop "CalendarCountdown"
sc.exe delete "CalendarCountdown"

```

---

### 4. Troubleshooting Guide

* **Missing Secrets / Authentication Failures:** * Verify `appsettings.Secrets.json` exists in `C:\path\CalendarCountdown`.
* Ensure `builder.Configuration.AddJsonFile(...)` is called in `Program.cs`.


* **Port Conflicts / URL Unreachable:** * Windows Services default to `http://localhost:5000`. To avoid conflicts, explicitly declare the port in your production `appsettings.json` located in the deployment folder:
`json { "Urls": "http://localhost:aaaaa" } `
* Restart the service (`sc.exe stop` then `sc.exe start`) to apply changes.


* **App Crashes Immediately (Trimming Issue):** * Because Razor Pages and Google SDKs use reflection, strict trimming might delete required code. If the executable crashes on load or during API calls, change `<PublishTrimmed>true</PublishTrimmed>` to `<TrimMode>partial</TrimMode>` in your `.csproj` and republish.