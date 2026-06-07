import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DatePipe } from '@angular/common';
import { Spinner } from './components/spinner/spinner';
import { Toast } from './components/toast/toast';

@NgModule({
  declarations: [Spinner, Toast],
  imports: [CommonModule, DatePipe],
  exports: [Spinner, Toast]
})
export class SharedModule {}
