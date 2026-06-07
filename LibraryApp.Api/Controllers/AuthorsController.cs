using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data.Services;
using Microsoft.AspNetCore.Mvc;

namespace LibraryApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
        => _authorService = authorService;

    // GET /api/authors
    [HttpGet]
    [ProducesResponseType<IEnumerable<AuthorDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var authors = await _authorService.GetAllAsync(ct);
        return Ok(authors);
    }

    // GET /api/authors/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType<AuthorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var author = await _authorService.GetByIdAsync(id, ct);
        return author is null ? NotFound() : Ok(author);
    }

    // POST /api/authors
    [HttpPost]
    [ProducesResponseType<CreateAuthorResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]   // duplicate name
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // validator
    public async Task<IActionResult> Create(
        [FromBody] CreateAuthorCommand command,
        CancellationToken ct)
    {
        // FluentValidation fires automatically before this method body runs
        // If invalid, 422 is returned automatically

        var result = await _authorService.CreateAsync(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.AuthorId },
            result);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType<AuthorDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateAuthorCommand command,
        CancellationToken ct)
    {
        var updated = await _authorService.UpdateAsync(id, command, ct);
        return Ok(updated);
    }
}