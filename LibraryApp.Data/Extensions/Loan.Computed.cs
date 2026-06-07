namespace LibraryApp.Data.Generated;

public partial class Loan
{
    public bool IsOverdue =>
        ReturnDate == null && DueDate < DateTime.UtcNow;

    public decimal CalculateFine(decimal dailyRate = 0.50m)
    {
        if (!IsOverdue) return 0;
        var overdueDays = (int)(DateTime.UtcNow - DueDate).TotalDays;
        return overdueDays * dailyRate;
    }
}