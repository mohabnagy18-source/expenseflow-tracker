namespace ExpenseFlow.Application.DTOs;

public record RegisterRequest(string FullName, string Email, string Password, string? Currency);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, string FullName, string Email, string Currency);
