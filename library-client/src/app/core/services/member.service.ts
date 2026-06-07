import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { MemberDto, CreateMemberCommand, CreateMemberResult, UpdateMemberCommand } from '../models/member.models';

@Injectable({ providedIn: 'root' })
export class MemberService {
  private readonly base = `${environment.apiUrl}/members`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<MemberDto[]> {
    return this.http.get<MemberDto[]>(this.base);
  }

  getById(id: number): Observable<MemberDto> {
    return this.http.get<MemberDto>(`${this.base}/${id}`);
  }

  create(command: CreateMemberCommand): Observable<CreateMemberResult> {
    return this.http.post<CreateMemberResult>(this.base, command);
  }

  update(id: number, command: UpdateMemberCommand): Observable<MemberDto> {
    return this.http.put<MemberDto>(`${this.base}/${id}`, command);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
