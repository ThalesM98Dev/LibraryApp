import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoansPageComponent } from './loans-page/loans-page.component';

const routes: Routes = [
  { path: '', component: LoansPageComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class LoansRoutingModule {}
