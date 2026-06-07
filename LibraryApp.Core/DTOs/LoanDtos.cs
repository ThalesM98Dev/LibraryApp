namespace LibraryApp.Core.DTOs;

// Response DTOs
public record ActiveLoanDto(
    int LoanId,
    string MemberName,
    string BookTitle,
    string AuthorName,
    DateTime DueDate,
    bool IsOverdue
);

public record LoanDto(
    int LoanId,
    int MemberId,
    int CopyId,
    DateTime LoanDate,
    DateTime DueDate
);

public record LoanExportDto(
    int LoanId,
    string MemberName,
    string BookTitle,
    string Isbn,
    string AuthorNames,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnDate,
    decimal Fine,
    string Status
);

// Request DTOs (Commands)
public record CheckOutCommand(int MemberId, int BookId);
public record ReturnCommand(int LoanId);
