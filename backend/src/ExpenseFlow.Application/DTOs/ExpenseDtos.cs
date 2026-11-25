namespace ExpenseFlow.Application.DTOs;

public record ExpenseDto(Guid Id, Guid CategoryId, string CategoryName, string CategoryIcon, string CategoryColor,
    decimal Amount, DateOnly Date, string? Note);
public record UpsertExpenseRequest(Guid CategoryId, decimal Amount, DateOnly Date, string? Note);
public record MonthlyReportDto(
    int Year, int Month, decimal Total, int ExpenseCount, decimal DailyAverage,
    IReadOnlyList<CategoryTotalDto> ByCategory,
    IReadOnlyList<DailyTotalDto> ByDay,
    decimal PreviousMonthTotal,
    decimal ChangePercent);
public record CategoryTotalDto(Guid CategoryId, string Name, string Icon, string Color, decimal Total, decimal Percent);
public record DailyTotalDto(DateOnly Date, decimal Total);
