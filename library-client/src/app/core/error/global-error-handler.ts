import { ErrorHandler, Injectable, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ToastService } from '../services/toast.service';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private toast = inject(ToastService);

  handleError(error: unknown): void {
    // HttpErrorResponse already handled by interceptor — skip
    if (error instanceof HttpErrorResponse) return;

    const message = error instanceof Error ? error.message : 'An unexpected error occurred.';
    console.error('[GlobalErrorHandler]', error);
    this.toast.error(message);
  }
}