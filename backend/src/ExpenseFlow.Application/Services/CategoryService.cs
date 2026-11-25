using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Application.Services;

public class CategoryService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.Categories.Where(c => c.UserId == userId).OrderBy(c => c.Name)
            .Select(c => new CategoryDto(c.Id, c.Name, c.Icon, c.Color, false)).ToListAsync(ct);

    public async Task<CategoryDto> CreateAsync(Guid userId, UpsertCategoryRequest request, CancellationToken ct = default)
    {
        var category = new Category { UserId = userId, Name = request.Name.Trim(), Icon = request.Icon, Color = request.Color };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Icon, category.Color, false);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var category = await db.Categories.Include(c => c.Expenses)
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);
        if (category is null) return false;
        if (category.Expenses.Count > 0) throw new InvalidOperationException("Category has expenses and cannot be deleted.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
