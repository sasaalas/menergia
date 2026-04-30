using Microsoft.AspNetCore.Mvc;
using menergia.api.Models;
using menergia.api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace menergia.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsumptionController : ControllerBase
{
    private readonly ConsumptionService _consumptionService;
    private readonly ILogger<ConsumptionController> _logger;

    public ConsumptionController(
        ConsumptionService consumptionService,
        ILogger<ConsumptionController> logger)
    {
        _consumptionService = consumptionService;
        _logger = logger;
    }

    /// <summary>
    /// Get current month consumption and costs
    /// </summary>
    /// <returns>Consumption data for the current month (April 1-29, 2026)</returns>
    [HttpGet("current-month")]
    [ProducesResponseType(typeof(ConsumptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public ActionResult<ConsumptionResponse> GetCurrentMonth()
    {
        try
        {
            var data = _consumptionService.GetCurrentMonthConsumption();

            if (data == null)
            {
                _logger.LogWarning("No consumption data available");
                return NotFound(new { message = "No consumption data available" });
            }

            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving consumption data");
            return StatusCode(500, new { message = "An error occurred while retrieving consumption data" });
        }
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
}
