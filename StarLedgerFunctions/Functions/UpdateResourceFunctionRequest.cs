using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StarLedgerFunctions.Models;

namespace StarLedgerFunctions.Functions;

public class UpdateResourceFunctionRequest
{
    private readonly ILogger<UpdateResourceFunctionRequest> _logger;

    public UpdateResourceFunctionRequest(ILogger<UpdateResourceFunctionRequest> logger)
    {
        _logger = logger;
    }

    [Function("UpdateResourceFunctionRequest")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest? req = null)
    {
        _logger.LogInformation("HTTP Update Resource was triggered");
        
        var result = await ResourceUpdater.UpdateResources(_logger);

        return new OkObjectResult("Resources Updated");
    }
}
