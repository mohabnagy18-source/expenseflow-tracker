using ExpenseFlow.Domain.Entities;

namespace ExpenseFlow.Application.Interfaces;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
