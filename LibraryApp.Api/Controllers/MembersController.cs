using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;

    public MembersController(IMemberService memberService)
        => _memberService = memberService;

    [HttpGet]
    [ProducesResponseType<IEnumerable<MemberDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var members = await _memberService.GetAllAsync(ct);
        return Ok(members);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType<MemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var member = await _memberService.GetByIdAsync(id, ct);
        return member is null ? NotFound() : Ok(member);
    }

    [HttpPost]
    [ProducesResponseType<CreateMemberResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateMemberCommand command,
        CancellationToken ct)
    {
        var result = await _memberService.CreateAsync(command, ct);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.MemberId },
            result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<MemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateMemberCommand command,
        CancellationToken ct)
    {
        var updated = await _memberService.UpdateAsync(id, command, ct);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _memberService.DeleteAsync(id, ct);
        return NoContent();
    }
}
