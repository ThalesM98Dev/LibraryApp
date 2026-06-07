import { NgModule } from '@angular/core';

import { LoansRoutingModule } from './loans-routing-module';
import { LoansPageComponent } from './loans-page/loans-page.component';

@NgModule({
  imports: [LoansRoutingModule, LoansPageComponent],
})
export class LoansModule {}
