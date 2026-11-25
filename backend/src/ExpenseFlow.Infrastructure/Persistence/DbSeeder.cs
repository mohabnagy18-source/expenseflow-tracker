using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, IPasswordHasher hasher)
    {
        await db.Database.MigrateAsync();

        var demo = await db.Users.FirstOrDefaultAsync(u => u.Email == "demo@expenseflow.com");
        if (demo is null)
        {
            demo = new User
            {
                FullName = "Demo User",
                Email = "demo@expenseflow.com",
                PasswordHash = hasher.Hash("Demo123!"),
                Currency = "EGP"
            };
            db.Users.Add(demo);
            var defaults = new (string Name, string Icon, string Color)[]
            {
                ("Groceries", "cart", "#22c55e"), ("Dining Out", "utensils", "#f97316"),
                ("Transport", "car", "#3b82f6"), ("Housing", "home", "#8b5cf6"),
                ("Utilities", "bolt", "#eab308"), ("Health", "heart", "#ef4444"),
                ("Entertainment", "film", "#ec4899"), ("Shopping", "bag", "#14b8a6"),
                ("Other", "tag", "#64748b")
            };
            foreach (var d in defaults)
                db.Categories.Add(new Category { UserId = demo.Id, Name = d.Name, Icon = d.Icon, Color = d.Color });
            await db.SaveChangesAsync();
        }

        if (!await db.Expenses.AnyAsync(e => e.UserId == demo.Id))
        {
            var cats = await db.Categories.Where(c => c.UserId == demo.Id).ToListAsync();
            Guid Cat(string name) => cats.First(c => c.Name == name).Id;
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var rnd = new Random(42);
            var samples = new (string Cat, decimal Min, decimal Max, string[] Notes)[]
            {
                ("Groceries", 150m, 900m, new[] { "Weekly supermarket run", "Fresh produce market", "Bulk staples" }),
                ("Dining Out", 80m, 450m, new[] { "Dinner with friends", "Coffee and dessert", "Friday takeout", "Business lunch" }),
                ("Transport", 30m, 250m, new[] { "Uber ride", "Metro card top-up", "Fuel" }),
                ("Utilities", 200m, 800m, new[] { "Electricity bill", "Internet bill", "Mobile plan" }),
                ("Entertainment", 100m, 500m, new[] { "Cinema tickets", "Streaming subscription", "Concert" }),
                ("Health", 150m, 700m, new[] { "Pharmacy", "Gym membership", "Dental checkup" }),
                ("Shopping", 300m, 1500m, new[] { "New sneakers", "Headphones", "Winter jacket" })
            };
            for (var monthOffset = 1; monthOffset >= 0; monthOffset--)
            {
                var monthDate = today.AddMonths(-monthOffset);
                var daysInMonth = DateTime.DaysInMonth(monthDate.Year, monthDate.Month);
                var lastDay = monthOffset == 0 ? today.Day : daysInMonth;
                var count = monthOffset == 0 ? 18 : 26;
                for (var i = 0; i < count; i++)
                {
                    var s = samples[rnd.Next(samples.Length)];
                    db.Expenses.Add(new Expense
                    {
                        UserId = demo.Id,
                        CategoryId = Cat(s.Cat),
                        Amount = Math.Round(s.Min + (decimal)rnd.NextDouble() * (s.Max - s.Min), 2),
                        Date = new DateOnly(monthDate.Year, monthDate.Month, rnd.Next(1, lastDay + 1)),
                        Note = s.Notes[rnd.Next(s.Notes.Length)],
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-rnd.Next(0, 30))
                    });
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
