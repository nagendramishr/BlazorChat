using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace BlazorChat.Provisioning.Func.Functions
{
    public class OrganizationFunctions
    {
        private readonly ILogger<OrganizationFunctions> _logger;

        public OrganizationFunctions(ILogger<OrganizationFunctions> logger)
        {
            _logger = logger;
        }

        [Function("CreateOrganization")]
        public IActionResult CreateOrganization([HttpTrigger(AuthorizationLevel.Function, "post", Route = "organizations")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request to create a organization.");
            return new OkObjectResult("CreateOrganization stub completed");
        }

        [Function("UpdateOrganizationStatus")]
        public IActionResult UpdateOrganizationStatus([HttpTrigger(AuthorizationLevel.Function, "patch", Route = "organizations/{id}/status")] HttpRequest req, string id)
        {
             _logger.LogInformation($"C# HTTP trigger function processed a request to update status for organization {id}.");
            return new OkObjectResult($"UpdateOrganizationStatus stub for {id}");
        }

        [Function("UpdateOrganizationConfig")]
        public IActionResult UpdateOrganizationConfig([HttpTrigger(AuthorizationLevel.Function, "put", Route = "organizations/{id}")] HttpRequest req, string id)
        {
             _logger.LogInformation($"C# HTTP trigger function processed a request to update config for organization {id}.");
            return new OkObjectResult($"UpdateOrganizationConfig stub for {id}");
        }

        [Function("GetOrganizations")]
        public IActionResult GetOrganizations([HttpTrigger(AuthorizationLevel.Function, "get", Route = "organizations")] HttpRequest req)
        {
             _logger.LogInformation("C# HTTP trigger function processed a request to list organizations.");
            return new OkObjectResult("GetOrganizations stub");
        }
    }
}
