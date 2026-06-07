import { Injectable } from '@angular/core';
import {
  HttpEvent, HttpHandler, HttpInterceptor,
  HttpRequest, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toast.service';

@Injectable()
export class ApiInterceptor implements HttpInterceptor {
  constructor(private toast: ToastService) {}

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    const apiReq = req.clone({
      setHeaders: { 'Content-Type': 'application/json' }
    });

    return next.handle(apiReq).pipe(
      catchError((err: HttpErrorResponse) => {
        const message = err.error?.title ?? err.error?.detail ?? 'An error occurred.';

        // Do not show toast for 404 — let component handle it
        if (err.status !== 404) {
          this.toast.error(message);
        }

        return throwError(() => err);
      })
    );
  }
}