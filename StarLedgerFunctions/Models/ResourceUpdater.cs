using System.Net;
using StarLedgerFunctions.Dtos;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace StarLedgerFunctions.Models;
internal static class ResourceUpdater
{
    public static async Task<HttpResponseMessage> UpdateResources(ILogger _logger)
    {
        //Create timer to check execution duration

        var uexApiKey = Environment.GetEnvironmentVariable("uexapikey");
        var uexCommodityUrl = Environment.GetEnvironmentVariable("uexcommodityurl");

        if (String.IsNullOrEmpty(uexApiKey))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable uexapikey");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        if (String.IsNullOrEmpty(uexCommodityUrl))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable uexcommodityurl");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var uexClient = new HttpClient();
        uexClient.DefaultRequestHeaders.Add("api_key", uexApiKey);
        var commodityEndpoint = new Uri(uexCommodityUrl);

        var response = await uexClient.GetAsync(commodityEndpoint);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (String.IsNullOrEmpty(responseBody))
        {
            _logger.LogWarning("Received no data from UEX API");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        List<StarLedgerResource> resourcesToUpdate;
        try
        {
            var uexResponse = JsonSerializer.Deserialize<UexResponse>(responseBody) ??
                              throw new InvalidOperationException();

            resourcesToUpdate = uexResponse.Data.Where(x => !String.IsNullOrEmpty(x.Code))
                .Select(r => new StarLedgerResource
                {
                    Name = r.Name,
                    Code = r.Code,
                    Type = r.Name.Contains("Ore") ? "Ore" : r.Kind,
                    PriceBuy = r.PriceBuy,
                    PriceSell = r.PriceSell
                }).ToList();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error deserializing API response");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var starLedgerUser = Environment.GetEnvironmentVariable("starledgeruser");
        var starLedgerPassword = Environment.GetEnvironmentVariable("starledgerpassword");
        var starLedgerBaseUrl = Environment.GetEnvironmentVariable("starLedgerBaseUrl");
        if (String.IsNullOrEmpty(starLedgerUser))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgeruser");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        if (String.IsNullOrEmpty(starLedgerPassword))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgerpassword");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        if (String.IsNullOrEmpty(starLedgerBaseUrl))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgerBaseUrl");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        var starLedgerClient = new HttpClient();
        starLedgerClient.BaseAddress = new Uri(starLedgerBaseUrl);

        var loginRequest = JsonSerializer.Serialize(new LoginRequestDto
        { Email = starLedgerUser, Password = starLedgerPassword });
        var loginResponse = await starLedgerClient.PostAsync("/identity/login",
            new StringContent(loginRequest, Encoding.UTF8, "application/json"));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();

        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginResponseBody);
        if (loginResult == null)
        {
            _logger.LogError("Error deserializing login response");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        starLedgerClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResult.AccessToken}");

        var resourcesRequest = JsonSerializer.Serialize(resourcesToUpdate);
        var resourcesResponse = await starLedgerClient.PostAsync("/resources",
            new StringContent(resourcesRequest, Encoding.UTF8, "application/json"));

        if (!resourcesResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Error updating resources to StarLedger.Api");
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }

        return resourcesResponse;
    }
}
