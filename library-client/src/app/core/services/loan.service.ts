import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ActiveLoan, CheckOutCommand, LoanDto } from '../models/loan.models';

@Injectable({ providedIn: 'root' })
export class LoanService {
  private readonly base = `${environment.apiUrl}/loans`;

  constructor(private http: HttpClient) {}

  getActiveLoans(): Observable<ActiveLoan[]> {
    return this.http.get<ActiveLoan[]>(`${this.base}/active`);
  }

  checkOut(command: CheckOutCommand): Observable<LoanDto> {
    return this.http.post<LoanDto>(`${this.base}/checkout`, command);
  }

  returnBook(loanId: number): Observable<void> {
    return this.http.patch<void>(`${this.base}/${loanId}/return`, {});
  }

  exportLoans(): Observable<Blob> {
    return this.http.get(`${this.base}/export`, { responseType: 'blob' });
  }
}