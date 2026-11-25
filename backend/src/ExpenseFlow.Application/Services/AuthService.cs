using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Application.Services;

public class AuthService(IApplicationDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
{
    private static readonly HashSet<string> AllowedCurrencies = new(StringComparer.OrdinalIgnoreCase) { "EGP", "USD", "EUR", "PLN", "GBP", "SAR", "AED" };

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("An account with this email already exists.");
        var currency = (request.Currency ?? "EGP").ToUpperInvariant();
        if (!AllowedCurrencies.Contains(currency)) currency = "EGP";

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            Currency = currency
        };
        db.Users.Add(user);

        var defaults = new (string Name, string Icon, string Color)[]
        {
            ("Groceries", "cart", "#22c55e"),
            ("Dining Out", "utensils", "#f97316"),
            ("Transport", "car", "#3b82f6"),
            ("Housing", "home", "#8b5cf6"),
            ("Utilities", "bolt", "#eab308"),
            ("Health", "heart", "#ef4444"),
            ("Entertainment", "film", "#ec4899"),
            ("Shopping", "bag", "#14b8a6"),
            ("Other", "tag", "#64748b")
        };
        foreach (var d in defaults)
            db.Categories.Add(new Category { UserId = user.Id, Name = d.Name, Icon = d.Icon, Color = d.Color });

        await db.SaveChangesAsync(ct);
        return new AuthResponse(jwt.CreateToken(user), user.FullName, user.Email, user.Currency);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");
        if (!hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");
        return new AuthResponse(jwt.CreateToken(user), user.FullName, user.Email, user.Currency);
    }
}
