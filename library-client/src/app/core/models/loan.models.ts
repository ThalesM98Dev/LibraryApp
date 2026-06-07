export interface ActiveLoan {
  loanId:     number;
  memberName: string;
  bookTitle:  string;
  authorName: string;
  dueDate:    string;
  isOverdue:  boolean;
  bookIsbn:   string;
  daysUntilDue: number;
}

export interface LoanDto {
  loanId:   number;
  memberId: number;
  copyId:   number;
  loanDate: string;
  dueDate:  string;
}

export interface CheckOutCommand {
  memberId: number;
  bookId:   number;
}
