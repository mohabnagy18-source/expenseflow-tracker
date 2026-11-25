using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Application.Services;

public class QuickItemService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<QuickItemDto>> ListAsync(Guid userId, CancellationToken ct = default)
        => await db.QuickItems.Include(q => q.Category)
            .Where(q => q.UserId == userId)
            .OrderBy(q => q.SortOrder).ThenBy(q => q.Name)
            .Select(q => new QuickItemDto(q.Id, q.CategoryId, q.Category.Name, q.Category.Icon, q.Category.Color,
                q.Name, q.Amount, q.Note))
            .ToListAsync(ct);

    public async Task<QuickItemDto> CreateAsync(Guid userId, UpsertQuickItemRequest request, CancellationToken ct = default)
    {
        var ownsCategory = await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, ct);
        if (!ownsCategory) throw new InvalidOperationException("Category not found.");
        if (request.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new InvalidOperationException("Name is required.");

        var maxOrder = await db.QuickItems.Where(q => q.UserId == userId)
            .MaxAsync(q => (int?)q.SortOrder, ct) ?? 0;
        var item = new QuickItem
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Name = name,
            Amount = request.Amount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            SortOrder = maxOrder + 1
        };
        db.QuickItems.Add(item);
        await db.SaveChangesAsync(ct);
        return (await ListAsync(userId, ct)).First(q => q.Id == item.Id);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await db.QuickItems.FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);
        if (item is null) return false;
        db.QuickItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<ExpenseDto?> LogAsync(Guid userId, Guid id, DateOnly? date, CancellationToken ct = default)
    {
        var item = await db.QuickItems.Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.Id == id && q.UserId == userId, ct);
        if (item is null) return null;
        var expense = new Expense
        {
            UserId = userId,
            CategoryId = item.CategoryId,
            Amount = item.Amount,
            Date = date ?? DateOnly.FromDateTime(DateTime.UtcNow),
            Note = string.IsNullOrWhiteSpace(item.Note) ? item.Name : item.Note
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return new ExpenseDto(expense.Id, expense.CategoryId, item.Category.Name, item.Category.Icon,
            item.Category.Color, expense.Amount, expense.Date, expense.Note);
    }
}
