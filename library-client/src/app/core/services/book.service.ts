import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthorOption,
  Book,
  BookSearchQuery,
  CategoryOption,
  CreateBookCommand,
  CreateBookResult,
  PagedResult,
  UpdateBookCommand,
} from '../models/book.models';

@Injectable({ providedIn: 'root' })
export class BookService {
  private readonly base = `${environment.apiUrl}/books`;

  constructor(private http: HttpClient) {}

  getById(id: number): Observable<Book> {
    return this.http.get<Book>(`${this.base}/${id}`);
  }

  searchBooks(query: BookSearchQuery): Observable<PagedResult<Book>> {
    return this.http.get<PagedResult<Book>>(`${this.base}`, {
      params: this.toHttpParams(query),
    });
  }
  createBook(command: CreateBookCommand): Observable<CreateBookResult> {
    const body = {
      isbn: command.isbn,
      title: command.title,
      publicationYear: Number(command.publicationYear),
      description: command.description ?? null,
      authorIds: (command.authorIds ?? []).map(Number),
      categoryIds: (command.categoryIds ?? []).map(Number),
      initialCopies: Number(command.initialCopies),
      copyCondition: command.copyCondition,
    };
    return this.http.post<CreateBookResult>(this.base, body);
  }

  getAuthors(): Observable<AuthorOption[]> {
    return this.http.get<AuthorOption[]>(`${environment.apiUrl}/authors`);
  }

  getCategories(): Observable<CategoryOption[]> {
    return this.http.get<CategoryOption[]>(`${environment.apiUrl}/categories`);
  }

  checkIsbnAvailability(isbn: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.base}/check-isbn`, {
      params: { isbn },
    });
  }

  updateBook(id: number, command: UpdateBookCommand): Observable<Book> {
    const body = {
      isbn: command.isbn,
      title: command.title,
      publicationYear: Number(command.publicationYear),
      description: command.description ?? null,
      authorIds: (command.authorIds ?? []).map(Number),
      categoryIds: (command.categoryIds ?? []).map(Number),
    };
    return this.http.put<Book>(`${this.base}/${id}`, body);
  }
  private toHttpParams(query: BookSearchQuery): HttpParams {
    let params = new HttpParams();
    if (query.title) params = params.set('title', query.title);
    if (query.authorName) params = params.set('authorName', query.authorName);
    if (query.categoryId != null) params = params.set('categoryId', query.categoryId);
    if (query.page != null) params = params.set('page', query.page);
    if (query.pageSize != null) params = params.set('pageSize', query.pageSize);
    return params;
  }
}
