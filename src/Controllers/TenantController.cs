using Microsoft.AspNetCore.Mvc;
using src.Models;
using src.Services;

namespace src.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TenantController : ControllerBase
{
    private readonly ITenantAdminService _adminService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantAdminService adminService, ILogger<TenantController> logger)
    {
        _adminService = adminService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<Organization>> CreateOrganization(Organization organization)
    {
        try
        {
            var result = await _adminService.OnboardOrganizationAsync(organization);
            return CreatedAtAction(nameof(GetOrganization), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(string id)
    {
        // We need a Get method in Admin Service or use List and filter
        // For now, let's use List to avoid changing interface too much, or direct Cosmos usage?
        // AdminService should probably expose Get.
        // But for now, let's implement List.
        var orgs = await _adminService.ListOrganizationsAsync();
        var org = orgs.FirstOrDefault(o => o.Id == id);
        if (org == null) return NotFound();
        return org;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Organization>>> ListOrganizations()
    {
        return Ok(await _adminService.ListOrganizationsAsync());
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrganization(string id, Organization organization)
    {
        if (id != organization.Id) return BadRequest("ID mismatch");

        try
        {
            await _adminService.UpdateOrganizationAsync(id, organization);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/disable")]
    public async Task<IActionResult> DisableOrganization(string id)
    {
        try
        {
            await _adminService.DisableOrganizationAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{id}/enable")]
    public async Task<IActionResult> EnableOrganization(string id)
    {
        try
        {
            await _adminService.EnableOrganizationAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
