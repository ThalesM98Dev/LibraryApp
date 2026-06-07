namespace LibraryApp.Data.Generated;

public partial class Member
{
    public bool IsActive => Status == "Active";
    public int ActiveLoanCount => Loans.Count(l => l.ReturnDate == null);
}