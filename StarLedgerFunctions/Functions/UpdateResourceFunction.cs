using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StarLedgerFunctions.Dtos;
using StarLedgerFunctions.Models;
using System.Text.Json;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace StarLedgerFunctions.Functions;

public class UpdateResourceFunction
{
    private readonly ILogger _logger;

    public UpdateResourceFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<UpdateResourceFunction>();
    }


    //* * * * * Every 1 minute
    //0 2 * * * Every day at 2am UTC
    [Function(nameof(UpdateResourceFunction))]
    public async Task Run([TimerTrigger("0 2 * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
        //Create timer to check execution duration

        var uexApiKey = Environment.GetEnvironmentVariable("uexapikey");
        var uexCommodityUrl = Environment.GetEnvironmentVariable("uexcommodityurl");

        if (String.IsNullOrEmpty(uexApiKey))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable uexapikey");
            return;
        }

        if (String.IsNullOrEmpty(uexCommodityUrl))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable uexcommodityurl");
            return;
        }

        var uexClient = new HttpClient();
        uexClient.DefaultRequestHeaders.Add("api_key", uexApiKey);
        var commodityEndpoint = new Uri(uexCommodityUrl);

        var response = await uexClient.GetAsync(commodityEndpoint);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (String.IsNullOrEmpty(responseBody))
        {
            _logger.LogWarning("Received no data from UEX API");
            return;
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
            return;
        }

        var starLedgerUser = Environment.GetEnvironmentVariable("starledgeruser");
        var starLedgerPassword = Environment.GetEnvironmentVariable("starledgerpassword");
        var starLedgerBaseUrl = Environment.GetEnvironmentVariable("starLedgerBaseUrl");
        if (String.IsNullOrEmpty(starLedgerUser))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgeruser");
            return;
        }
        if (String.IsNullOrEmpty(starLedgerPassword))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgerpassword");
            return;
        }
        if (String.IsNullOrEmpty(starLedgerBaseUrl))
        {
            _logger.LogCritical("Couldn't retrieve Environment variable starledgerBaseUrl");
            return;
        }

        var starLedgerClient = new HttpClient();
        starLedgerClient.BaseAddress = new Uri(starLedgerBaseUrl);

        var loginRequest = JsonSerializer.Serialize(new LoginRequestDto { Email = starLedgerUser, Password = starLedgerPassword });
        var loginResponse = await starLedgerClient.PostAsync("/identity/login", new StringContent(loginRequest, Encoding.UTF8, "application/json"));
        var loginResponseBody = await loginResponse.Content.ReadAsStringAsync();

        var loginResult = JsonSerializer.Deserialize<LoginResponseDto>(loginResponseBody);
        if (loginResult == null)
        {
            _logger.LogError("Error deserializing login response");
            return;
        }

        starLedgerClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {loginResult.AccessToken}");

        var resourcesRequest = JsonSerializer.Serialize(resourcesToUpdate);
        var resourcesResponse = await starLedgerClient.PostAsync("/resources", new StringContent(resourcesRequest, Encoding.UTF8, "application/json"));

        if (!resourcesResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Error updating resources to StarLedger.Api");
            return;
        }
    }
}