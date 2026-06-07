import { Injectable, signal } from '@angular/core';
import { catchError, finalize, tap } from 'rxjs/operators';
import { BookService } from '../services/book.service';
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
import { forkJoin, Observable, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

@Injectable({ providedIn: 'root' })
export class BookStateService {
  readonly results = signal<PagedResult<Book> | null>(null);
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isCreating = signal(false);
  readonly createError = signal<string | null>(null);
  readonly authors = signal<AuthorOption[]>([]);
  readonly categories = signal<CategoryOption[]>([]);
  readonly books = signal<Book[]>([]);
  readonly isLoadingReferenceData = signal(false);
  readonly isUpdating = signal(false);
  readonly updateError = signal<string | null>(null);
  // Added for add-book component
  readonly lastCreatedBookId = signal<number | null>(null);
  
  constructor(private bookService: BookService, private toastService: ToastService) {}

  loadReferenceData(): void {
    // Load authors and categories in parallel for the form dropdowns
    this.isLoadingReferenceData.set(true);
    forkJoin({
      authors: this.bookService.getAuthors(),
      categories: this.bookService.getCategories(),
    }).subscribe({
      next: ({ authors, categories }) => {
        this.authors.set(authors);
        this.categories.set(categories);
        this.isLoadingReferenceData.set(false);
      },
      error: () => {
        this.isLoadingReferenceData.set(false);
      },
    });
  }
  createBook(command: CreateBookCommand): Observable<CreateBookResult> {
    this.isCreating.set(true);
    this.createError.set(null);

    return this.bookService.createBook(command).pipe(
      tap((result) => {
        // Store the created book ID for success message
        this.lastCreatedBookId.set(result.bookId);
        // @ts-ignore
        this.toastService.success(`"${result.title}" added with ${result.copiesCreated} copies.`);
      }),
      finalize(() => this.isCreating.set(false)),
      catchError((err) => {
        const msg = err.error?.title ?? 'Failed to create book.';
        this.createError.set(msg);
        return throwError(() => err);
      }),
    );
  }
  clearError() {
    this.errorMessage.set(null);
    this.createError.set(null);
    this.updateError.set(null);
  }
  search(query: BookSearchQuery): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.bookService
      .searchBooks(query)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (result) => this.results.set(result),
        error: (err) => this.errorMessage.set(err.message ?? 'Search failed'),
      });
  }
  updateBook(id: number, command: UpdateBookCommand): Observable<Book> {
    this.isUpdating.set(true);
    this.updateError.set(null);

    return this.bookService.updateBook(id, command).pipe(
      tap((updated) => {
        // Patch the book in the local list so the catalogue
        // reflects the change immediately without a full reload.
        this.books.update((list: Book[]) => list.map((b: Book) => (b.bookId === updated.bookId ? updated : b)));
        // @ts-ignore
        this.toastService.success(`"${updated.title}" updated successfully.`);
      }),
      finalize(() => this.isUpdating.set(false)),
      catchError((err) => {
        const msg = err.error?.title ?? 'Failed to update book.';
        this.updateError.set(msg);
        return throwError(() => err);
      }),
    );
  }
}
