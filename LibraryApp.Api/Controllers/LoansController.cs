using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly ILoanService _loanService;

    public LoansController(ILoanService loanService) => _loanService = loanService;

    [HttpGet("active")]
    [ProducesResponseType<IEnumerable<ActiveLoanDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var loans = await _loanService.GetActiveLoansAsync(ct);
        return Ok(loans);
    }

    [HttpPost("checkout")]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutCommand command, CancellationToken ct)
    {
        var loan = await _loanService.CheckOutAsync(command, ct);
        return CreatedAtAction(nameof(GetActive), new { loanId = loan.LoanId }, loan);
    }

    [HttpPatch("{loanId:int}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Return(int loanId, CancellationToken ct)
    {
        await _loanService.ReturnAsync(loanId, ct);
        return NoContent();
    }

    [HttpGet("export")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var bytes = await _loanService.ExportLoansToCsvAsync(ct);
        return File(bytes, "text/csv", $"loans-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }
}
