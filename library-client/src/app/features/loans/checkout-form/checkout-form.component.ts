import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { LoanStateService } from '../../../core/state/loan-state.service';

@Component({
  selector: 'app-checkout-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './checkout-form.component.html',
  styleUrl: './checkout-form.component.scss',
})
export class CheckoutFormComponent {
  private readonly fb = inject(FormBuilder);
  readonly state = inject(LoanStateService);

  readonly isSubmitting = signal(false);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  form = this.fb.group({
    memberId: [null as number | null, [Validators.required, Validators.min(1)]],
    bookId: [null as number | null, [Validators.required, Validators.min(1)]],
  });

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.successMessage.set(null);
    this.errorMessage.set(null);

    this.state.checkOut(this.form.getRawValue() as { memberId: number; bookId: number }).subscribe({
      next: loan => {
        this.successMessage.set(`Loan #${loan.loanId} created. Due: ${new Date(loan.dueDate).toLocaleDateString()}`);
        this.form.reset();
        this.isSubmitting.set(false);
      },
      error: err => {
        this.errorMessage.set(err?.message || 'Failed to check out book. Please try again.');
        this.isSubmitting.set(false);
      },
    });
  }

  onReset(): void {
    this.form.reset();
    this.successMessage.set(null);
    this.errorMessage.set(null);
  }

  clearError(): void {
    this.errorMessage.set(null);
  }

  clearSuccess(): void {
    this.successMessage.set(null);
  }
}
