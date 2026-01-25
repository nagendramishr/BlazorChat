using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BlazorChat.Shared.Models;
using src.Services;
using src.Services.Application;
using System.Security.Claims;

namespace src.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "GlobalAdmin")]
public class OrganizationController : ControllerBase
{
    private readonly IOrganizationApplicationService _orgAppService;
    private readonly ILogger<OrganizationController> _logger;

    public OrganizationController(IOrganizationApplicationService orgAppService, ILogger<OrganizationController> logger)
    {
        _orgAppService = orgAppService;
        _logger = logger;
    }

    private string GetCurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    [HttpPost]
    public async Task<ActionResult<Organization>> CreateOrganization(CreateOrganizationRequest request)
    {
        var result = await _orgAppService.CreateOrganizationAsync(request, GetCurrentUserId());
        
        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return CreatedAtAction(nameof(GetOrganization), new { id = result.Organization!.Id }, result.Organization);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(string id)
    {
        var org = await _orgAppService.GetOrganizationAsync(id);
        if (org == null) return NotFound();
        return org;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> ListOrganizations()
    {
        return Ok(await _orgAppService.ListOrganizationsAsync());
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Organization>> UpdateOrganization(string id, UpdateOrganizationRequest request)
    {
        var result = await _orgAppService.UpdateOrganizationAsync(id, request, GetCurrentUserId());
        
        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { errors = result.Errors });
            }
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Organization);
    }

    [HttpPost("{id}/disable")]
    public async Task<ActionResult<Organization>> DisableOrganization(string id, [FromBody] DisableOrganizationRequest? request = null)
    {
        var result = await _orgAppService.DisableOrganizationAsync(id, request?.Reason, GetCurrentUserId());
        
        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { errors = result.Errors });
            }
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Organization);
    }

    [HttpPost("{id}/enable")]
    public async Task<ActionResult<Organization>> EnableOrganization(string id)
    {
        var result = await _orgAppService.EnableOrganizationAsync(id, GetCurrentUserId());
        
        if (!result.Success)
        {
            if (result.Errors.Any(e => e.Contains("not found")))
            {
                return NotFound(new { errors = result.Errors });
            }
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result.Organization);
    }
}

/// <summary>
/// Request model for disabling an organization.
/// </summary>
public class DisableOrganizationRequest
{
    public string? Reason { get; set; }
}
