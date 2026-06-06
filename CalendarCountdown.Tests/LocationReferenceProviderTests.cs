using System.Net;
using CalendarCountdown.Solution.Services;
using FluentAssertions;

namespace CalendarCountdown.Tests;

public class LocationReferenceProviderTests
{
    // 1. Create a lightweight mock handler using C# primary constructors
    private class MockRedirectHandler(Uri finalUri) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Simulate the HTTP response where the RequestMessage contains the final redirected URI
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = new HttpRequestMessage(HttpMethod.Get, finalUri)
            };

            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task ExtractQueryData_DoubleEncodedGoogleLinkInsideConsent_FindPlaceName()
    {
        var initialUrl = "https://goo.gl/maps/dummyShortLink";
        var simulatedFinalUrl = new Uri("https://consent.google.com/ml?continue=https://maps.google.com/maps?q%3DSalle%2BCommunale%2Bdu%2BPetit-Lancy,%2BPetit-Lancy,%2BAv.%2BLouis-Bertrand%2B7-9,%2B1213%2BPetit-Lancy,%2BSwitzerland%26ftid%3D0x478c7b379a8fbf6b:0x6eeb13738b35d2e8%26entry%3Dgps%26shh%3DCAE%26lucs%3D,94297699,94284493,94231188,94280568,47071704,94218641,94282134,94286869%26g_ep%3DCAISEjI2LjE4LjAuOTA2NTA0NDMzMBgAIIgnKkgsOTQyOTc2OTksOTQyODQ0OTMsOTQyMzExODgsOTQyODA1NjgsNDcwNzE3MDQsOTQyMTg2NDEsOTQyODIxMzQsOTQyODY4NjlCAlVB%26skid%3D355af970-7ffa-4801-876d-53aaa8150ddc%26g_st%3Dic&gl=CH&m=0&pc=m&uxe=eomtm&cm=2&hl=de&src=1");

        var handler = new MockRedirectHandler(simulatedFinalUrl);
        using var httpClient = new HttpClient(handler);
        var provider = new LocationReferenceProvider(httpClient);

        var result = await provider.ExtractQueryData(initialUrl);

        result.Should().BeEquivalentTo("https://maps.google.com/maps?q=Salle Communale du Petit-Lancy, Petit-Lancy, Av. Louis-Bertrand 7-9, 1213 Petit-Lancy, Switzerland&output=embed");
    }
}