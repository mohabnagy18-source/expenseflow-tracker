using ExpenseFlow.Application.Interfaces;
using ExpenseFlow.Application.Services;
using ExpenseFlow.Infrastructure.Persistence;
using ExpenseFlow.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExpenseFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddExpenseFlowServices(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<ExpenseService>();
        services.AddScoped<CategoryService>();
        services.AddScoped<QuickItemService>();
        return services;
    }

    public static IServiceCollection AddExpenseFlowInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        return services;
    }
}
