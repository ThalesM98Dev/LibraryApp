import { Component, inject } from '@angular/core';
import { CheckoutFormComponent } from '../checkout-form/checkout-form.component';
import { LoanListComponent } from '../loan-list/loan-list.component';
import { LoanStateService } from '../../../core/state/loan-state.service';

@Component({
  selector: 'app-loans-page',
  imports: [CheckoutFormComponent, LoanListComponent],
  templateUrl: './loans-page.component.html',
  styleUrl: './loans-page.component.scss',
})
export class LoansPageComponent {
  readonly state = inject(LoanStateService);

  onExport(): void {
    this.state.exportLoans();
  }
}
