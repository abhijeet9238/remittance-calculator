using Microsoft.AspNetCore.Mvc;

namespace RemittanceCalculatorApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route builds cleanly to: /api/remittance/calculate
    public class RemittanceController : ControllerBase
    {
        private readonly RemittanceEngine _engine;

        public RemittanceController(RemittanceEngine engine)
        {
            _engine = engine;
        }

        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] RemittanceRequest request)
        {
            if (request.Amount <= 0)
            {
                return BadRequest("Transfer amount must be greater than zero.");
            }

            try
            {
                // Execute calculation cleanly via the stateless service engine
                var breakdown = await _engine.ComputeNetPayoutAsync(request);
                return Ok(breakdown);
            }
            catch (KeyNotFoundException ex)
            {
                // Gracefully inform user if they picked an exotic currency pair that has zero active trade volume
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An internal processing error occurred.", details = ex.Message });
            }
        }
    }
}