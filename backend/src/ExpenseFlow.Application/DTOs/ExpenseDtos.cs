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
public record QuickItemDto(Guid Id, Guid CategoryId, string CategoryName, string CategoryIcon, string CategoryColor,
    string Name, decimal Amount, string? Note);
public record UpsertQuickItemRequest(Guid CategoryId, string Name, decimal Amount, string? Note);
public record LogQuickItemRequest(DateOnly? Date);
public record WeeklyReportDto(
    DateOnly WeekStart, DateOnly WeekEnd, decimal Total, int ExpenseCount, decimal DailyAverage,
    IReadOnlyList<CategoryTotalDto> ByCategory,
    IReadOnlyList<DailyTotalDto> ByDay,
    decimal PreviousWeekTotal,
    decimal ChangePercent,
    decimal TodayTotal, decimal YesterdayTotal, decimal DayChangePercent);
