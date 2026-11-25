using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<QuickItem> QuickItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
