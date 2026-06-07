import { Injectable, signal, computed } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { LoanService } from '../services/loan.service';
import { ActiveLoan, CheckOutCommand, LoanDto } from '../models/loan.models';
import { ToastService } from '../services/toast.service';

@Injectable({ providedIn: 'root' })
export class LoanStateService {
  // State signals
  readonly activeLoans  = signal<ActiveLoan[]>([]);
  readonly isLoading    = signal(false);
  readonly exporting    = signal(false);
  readonly errorMessage = signal<string | null>(null);

  // Derived (computed) signals — automatically reactive
  readonly overdueCount = computed(() =>
    this.activeLoans().filter(l => l.isOverdue).length
  );
  readonly totalActive = computed(() => this.activeLoans().length);

  constructor(private loanService: LoanService, private toastService: ToastService) {}

  loadActiveLoans(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.loanService.getActiveLoans().pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next:  loans => this.activeLoans.set(loans),
      error: err   => this.errorMessage.set(err.message)
    });
  }

  checkOut(command: CheckOutCommand): Observable<LoanDto> {
    return this.loanService.checkOut(command).pipe(
      tap((result) => {
        // Show success message
        this.toastService.success(`Loan #${result.loanId} created. Due: ${new Date(result.dueDate).toLocaleDateString()}`);
        this.loadActiveLoans();  // refresh after checkout
      }),
      catchError((err) => {
        this.errorMessage.set(err.message ?? 'Failed to check out book');
        throw err;
      })
    );
  }

  returnBook(loanId: number): Observable<void> {
    return this.loanService.returnBook(loanId).pipe(
      tap(() => {
        this.toastService.success('Book returned successfully');
        this.loadActiveLoans();
      }),
      catchError((err) => {
        this.errorMessage.set(err.message ?? 'Failed to return book');
        throw err;
      })
    );
  }

  exportLoans(): void {
    this.exporting.set(true);
    this.errorMessage.set(null);

    this.loanService.exportLoans().pipe(
      finalize(() => this.exporting.set(false))
    ).subscribe({
      next: blob => this.downloadBlob(blob),
      error: err => {
        this.errorMessage.set(err.message ?? 'Failed to export loans');
        this.toastService.error('Failed to export loans');
      }
    });
  }

  private downloadBlob(blob: Blob): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'loans-export.csv';
    a.style.display = 'none';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
    this.toastService.success('Loans exported successfully');
  }

  clearError() {
    this.errorMessage.set(null);
  }
}