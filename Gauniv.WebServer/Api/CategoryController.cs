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
    private readonly CategoryService _categoryService = categoryService;

    [HttpPost]
    public async Task<ActionResult<CategoryFullDto>> AddCategory(CategoryCreateOrEditDto category)
    {
        var createdCategory = await _categoryService.AddCategoryAsync(category);
        return CreatedAtAction(nameof(GetAllCategories), new { id = createdCategory.Id }, createdCategory);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        if (!result) return NotFound();
        return NoContent();
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult<CategoryFullDto>> UpdateCategory(int id, CategoryCreateOrEditDto category)
    {
        var updatedCategory = await _categoryService.UpdateCategoryAsync(id, category);
        if (updatedCategory == null) return NotFound();
        return Ok(updatedCategory);
    }

    [HttpGet("all")]
    public async Task<ActionResult<List<CategoryFullDto>>> GetAllCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(categories);
    }
}