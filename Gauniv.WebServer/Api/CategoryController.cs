using Gauniv.WebServer.Dtos;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gauniv.WebServer.Api;

[Route("category")]
[ApiController]
public class CategoryController(
    CategoryService categoryService,
    MappingProfile mappingProfile) : ControllerBase
{
    [HttpGet("all")]
    public async Task<ActionResult<List<CategoryFullDto>>> GetAllCategories()
    {
        var categories = await categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }
}