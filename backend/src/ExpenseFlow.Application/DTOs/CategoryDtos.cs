namespace ExpenseFlow.Application.DTOs;

public record CategoryDto(Guid Id, string Name, string Icon, string Color, bool IsDefault);
public record UpsertCategoryRequest(string Name, string Icon, string Color);
