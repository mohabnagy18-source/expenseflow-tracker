using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Application.Services;

public class ExpenseService(IApplicationDbContext db)
{
    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(Guid userId, int? year, int? month, Guid? categoryId, CancellationToken ct = default)
    {
        var query = db.Expenses.Include(e => e.Category).Where(e => e.UserId == userId);
        if (year is { } y) query = query.Where(e => e.Date.Year == y);
        if (month is { } m) query = query.Where(e => e.Date.Month == m);
        if (categoryId is { } c) query = query.Where(e => e.CategoryId == c);
        var items = await query.OrderByDescending(e => e.Date).ThenByDescending(e => e.CreatedAtUtc).ToListAsync(ct);
        return items.Select(ToDto).ToList();
    }

    public async Task<ExpenseDto> CreateAsync(Guid userId, UpsertExpenseRequest request, CancellationToken ct = default)
    {
        var ownsCategory = await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, ct);
        if (!ownsCategory) throw new InvalidOperationException("Category not found.");
        if (request.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");

        var expense = new Expense
        {
            UserId = userId,
            CategoryId = request.CategoryId,
            Amount = request.Amount,
            Date = request.Date,
            Note = request.Note
        };
        db.Expenses.Add(expense);
        await db.SaveChangesAsync(ct);
        return (await ListAsync(userId, null, null, null, ct)).First(e => e.Id == expense.Id);
    }

    public async Task<ExpenseDto?> UpdateAsync(Guid userId, Guid id, UpsertExpenseRequest request, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
        if (expense is null) return null;
        var ownsCategory = await db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.UserId == userId, ct);
        if (!ownsCategory) throw new InvalidOperationException("Category not found.");
        if (request.Amount <= 0) throw new InvalidOperationException("Amount must be greater than zero.");
        expense.CategoryId = request.CategoryId;
        expense.Amount = request.Amount;
        expense.Date = request.Date;
        expense.Note = request.Note;
        await db.SaveChangesAsync(ct);
        return (await ListAsync(userId, null, null, null, ct)).First(e => e.Id == expense.Id);
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var expense = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId, ct);
        if (expense is null) return false;
        db.Expenses.Remove(expense);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<MonthlyReportDto> MonthlyReportAsync(Guid userId, int year, int month, CancellationToken ct = default)
    {
        var expenses = await db.Expenses.Include(e => e.Category)
            .Where(e => e.UserId == userId && e.Date.Year == year && e.Date.Month == month)
            .ToListAsync(ct);
        var (prevYear, prevMonth) = month == 1 ? (year - 1, 12) : (year, month - 1);
        var prevTotal = await db.Expenses
            .Where(e => e.UserId == userId && e.Date.Year == prevYear && e.Date.Month == prevMonth)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var total = expenses.Sum(e => e.Amount);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var byCategory = expenses
            .GroupBy(e => new { e.CategoryId, e.Category.Name, e.Category.Icon, e.Category.Color })
            .Select(g => new CategoryTotalDto(g.Key.CategoryId, g.Key.Name, g.Key.Icon, g.Key.Color,
                g.Sum(e => e.Amount), total == 0 ? 0 : Math.Round(g.Sum(e => e.Amount) / total * 100, 1)))
            .OrderByDescending(c => c.Total).ToList();
        var byDay = Enumerable.Range(1, daysInMonth)
            .Select(d => new DailyTotalDto(new DateOnly(year, month, d),
                expenses.Where(e => e.Date.Day == d).Sum(e => e.Amount)))
            .ToList();
        var change = prevTotal == 0 ? 0 : Math.Round((total - prevTotal) / prevTotal * 100, 1);

        return new MonthlyReportDto(year, month, total, expenses.Count,
            Math.Round(total / daysInMonth, 2), byCategory, byDay, prevTotal, change);
    }

    private static ExpenseDto ToDto(Expense e) =>
        new ExpenseDto(e.Id, e.CategoryId, e.Category.Name, e.Category.Icon, e.Category.Color,
            e.Amount, e.Date, e.Note);
}
