using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services;

public class CategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryFullDto> AddCategoryAsync(CategoryCreateOrEditDto categoryDto)
    {
        // ValidationHelper.Validate(categoryDto); // Assuming ValidationHelper exists and is needed, matching GameService
        var category = categoryDto.Adapt<Category>();
        
        var addedCategory = _context.Set<Category>().Add(category);
        await _context.SaveChangesAsync();

        return addedCategory.Entity.Adapt<CategoryFullDto>();
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.Set<Category>().FindAsync(categoryId);
        if (category == null) return false;

        _context.Set<Category>().Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CategoryFullDto?> UpdateCategoryAsync(int categoryId, CategoryCreateOrEditDto categoryDto)
    {
        // ValidationHelper.Validate(categoryDto);
        var existingCategory = await _context.Set<Category>().FindAsync(categoryId);
        if (existingCategory == null) return null;

        categoryDto.Adapt(existingCategory);

        await _context.SaveChangesAsync();
        return existingCategory.Adapt<CategoryFullDto>();
    }

    public async Task<List<CategoryFullDto>> GetAllCategoriesAsync()
    {
        return await _context.Set<Category>()
            .OrderBy(c => c.Title)
            .ProjectToType<CategoryFullDto>()
            .ToListAsync();
    }
}
