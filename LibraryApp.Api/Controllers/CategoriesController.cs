using LibraryApp.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApp.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly LibraryContext _ctx;
        public CategoriesController(LibraryContext ctx) => _ctx = ctx;

        [HttpGet]
        public async Task<IActionResult> GetAll(CancellationToken ct)
            => Ok(await _ctx.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.CategoryId, c.Name })
                .ToListAsync(ct));
    }
}
