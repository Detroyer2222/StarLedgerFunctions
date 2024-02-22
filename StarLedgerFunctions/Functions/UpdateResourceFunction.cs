using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StarLedgerFunctions.Models;

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
    public async Task Run([TimerTrigger("0 0 2 ? * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }

        var result = await ResourceUpdater.UpdateResources(_logger);

        if (result.IsSuccessStatusCode)
        {
            _logger.LogInformation("Resource update was successful");
        }
        else
        {
            _logger.LogError("Resource update failed");
        }
    }


}