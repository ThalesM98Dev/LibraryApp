import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { LoanStateService } from '../../../core/state/loan-state.service';

@Component({
  selector: 'app-loan-list',
  imports: [CommonModule],
  templateUrl: './loan-list.component.html',
  styleUrl: './loan-list.component.scss',
})
export class LoanListComponent implements OnInit {
  readonly state = inject(LoanStateService);

  ngOnInit(): void {
    this.state.loadActiveLoans();
  }

  onReturn(loanId: number): void {
    if (!confirm('Confirm return?')) return;
    this.state.returnBook(loanId).subscribe();
  }
}
