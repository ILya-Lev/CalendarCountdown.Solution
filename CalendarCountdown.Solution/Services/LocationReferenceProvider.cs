using CalendarCountdown.Solution.Entities;
using System.Net;
using System.Text.RegularExpressions;

namespace CalendarCountdown.Solution.Services;

public interface ILocationReferenceProvider
{
    Task<LocationReference?> ParseLocationData(string? locationData, string? descriptionData);
}

public partial class LocationReferenceProvider(HttpClient httpClient) : ILocationReferenceProvider
{
    // Matches standard map links, shortened goo.gl links, and googleusercontent proxies
    [GeneratedRegex(
        @"https?:\/\/(?:www\.)?(?:maps\.google\.com|google\.com\/maps|goo\.gl\/maps|maps\.app\.goo\.gl|googleusercontent\.com)[^\s<""]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 1_000)]
    private static partial Regex GetMapRegex();

    // Extracts the location name or coordinates from the expanded Google Maps URL
    [GeneratedRegex(@"(?:/place/|/search/|[\?&]q=)([^/&\?]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetPlaceOrQueryRegex();

    [GeneratedRegex(@"@(-?\d+\.\d+,-?\d+\.\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex GetCoordinatesRegex();

    public async Task<LocationReference?> ParseLocationData(string? locationData, string? descriptionData) 
        => await ParseLocation(locationData) 
        ?? await ParseDescription(descriptionData);

    private async Task<LocationReference?> ParseLocation(string? locationData)
    {
        if (string.IsNullOrWhiteSpace(locationData)) return null;

        var match = GetMapRegex().Match(locationData);

        return match.Success
            ? await ExtractUrl(match.Value)              //location data might be a URL of the place
            : new(                  //location data is a name of the place
                DisplayLocationText: locationData,
                MapExternalUrl: $"https://maps.google.com/maps?q={Uri.EscapeDataString(locationData)}",
                MapIframeSrc: $"https://maps.google.com/maps?q={Uri.EscapeDataString(locationData)}&output=embed"
            );
    }

    private async Task<LocationReference?> ParseDescription(string? descriptionData)
    {
        if (string.IsNullOrWhiteSpace(descriptionData)) return null;

        var match = GetMapRegex().Match(descriptionData);

        return match.Success ? await ExtractUrl(match.Value) : null;
    }

    private async Task<LocationReference> ExtractUrl(string rawUrl)
    {
        // Clean up trailing punctuation that Regex might catch from a plain text paragraph
        var url = rawUrl.TrimEnd('.', ',', ')', ']', '!', '\'', '"');

        var mapIframeSrc = url.Contains("q=") || url.Contains("/embed?")
            ? url + (url.Contains("?") ? "&output=embed" : "?output=embed") // If it is a full query URL, we can safely embed it. 
            : await ExtractQueryData(url); // If it is a shortened link (maps.app.goo.gl), embedding fails. 

        return new("Location provided via Maps link", url, mapIframeSrc);
    }

    internal async Task<string?> ExtractQueryData(string url)
    {
        // Resolve shortened link via HTTP GET (Headers only to save bandwidth)
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead); //last option reduces traffic
            var expandedUrl = response.RequestMessage?.RequestUri?.ToString() ?? url;
            expandedUrl = WebUtility.UrlDecode(WebUtility.UrlDecode(expandedUrl));

            // Try to extract location name from /place/ or ?q=
            var queryData = GetPlaceOrQueryRegex().Match(expandedUrl).Groups[1].Value;

            // Fallback: Try to extract raw coordinates from @lat,lng
            if (string.IsNullOrEmpty(queryData))
            {
                queryData = GetCoordinatesRegex().Match(expandedUrl).Groups[1].Value;
            }

            return string.IsNullOrEmpty(queryData)
                ? null
                : $"https://maps.google.com/maps?q={queryData}&output=embed";

        }
        catch
        {
            // Network failure or invalid URL format; fallback to button-only UI
            return null;
        }
    }
}
