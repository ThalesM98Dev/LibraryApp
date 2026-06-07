using LibraryApp.Core.DTOs;
using LibraryApp.Core.Interfaces;
using LibraryApp.Data.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IBookRepository _books;
    private readonly IBookService _bookService;

    public BooksController(IBookRepository books, IBookService bookService)
    {
        _books = books;
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] BookSearchQuery query, CancellationToken ct)
        => Ok(await _books.SearchAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var book = await _books.GetByIdAsync(id, ct);
        return book is null ? NotFound() : Ok(book);
    }

    [HttpPost]
    [ProducesResponseType<CreateBookResult>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]   // duplicate ISBN
    [ProducesResponseType(StatusCodes.Status404NotFound)]   // bad author/category IDs
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // validator
    public async Task<IActionResult> Create(
            [FromBody] CreateBookCommand command,
            CancellationToken ct)
    {
        // FluentValidation fires before this method body runs.
        // If validation fails, the framework returns 422 automatically.

        var result = await _bookService.CreateAsync(command, ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.BookId },
            result);
    }
    // PUT /api/books/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType<BookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]   // book not found
    [ProducesResponseType(StatusCodes.Status409Conflict)]   // ISBN taken by another book
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)] // validator
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateBookCommand command,
        CancellationToken ct)
    {
        // FluentValidation runs before this method body.
        // If the payload is invalid, 422 is returned automatically.

        var updated = await _bookService.UpdateAsync(id, command, ct);
        return Ok(updated);
    }
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)] // if active loans
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _bookService.DeleteAsync(id, ct);
        return NoContent();
    }
}