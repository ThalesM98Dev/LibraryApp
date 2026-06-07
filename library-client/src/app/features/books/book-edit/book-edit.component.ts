import { Component, OnInit, OnDestroy, inject, ChangeDetectorRef, ViewChild, ElementRef, HostListener, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  Validators,
  AbstractControl,
  ValidationErrors,
  AsyncValidatorFn,
  ReactiveFormsModule,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, Observable, of } from 'rxjs';
import {
  switchMap,
  map,
  catchError,
  takeUntil,
  debounceTime,
  filter,
} from 'rxjs/operators';
import { BookStateService } from '../../../core/state/book-state.service';
import { BookService } from '../../../core/services/book.service';
import { Book, UpdateBookCommand } from '../../../core/models/book.models';

@Component({
  selector: 'app-edit-book',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './book-edit.component.html',
  styleUrls: ['./book-edit.component.scss'],
})
export class BookEditComponent implements OnInit, OnDestroy {
  // ── dependencies ────────────────────────────────────────────────────
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly bookService = inject(BookService);
  readonly state = inject(BookStateService);
  private readonly cdr = inject(ChangeDetectorRef);

  // ── view state ───────────────────────────────────────────────────────
  isLoadingBook = true;
  loadError: string | null = null;
  bookId!: number;
  originalIsbn = '';
  originalFormValues: any = null; // store pristine values for reset
  private bookRaw: Book | null = null; // raw API response, before name→ID mapping

  readonly currentYear = new Date().getFullYear();
  readonly maxDescLen = 4000;
  readonly descriptionWarnThreshold = 3600; // warn at 90%

  toastMessage: string | null = null;
  private toastTimeout: any;

  // ── UI state ─────────────────────────────────────────────────────────
  focusedField = '';
  showAuthorSuggestions = false;
  showCategorySuggestions = false;
  searchAuthorTerm = '';
  searchCategoryTerm = '';
  filteredAuthors: any[] = [];
  filteredCategories: any[] = [];

  @ViewChild('pageTitle') pageTitle!: ElementRef;

  private readonly destroy$ = new Subject<void>();

  // ── reactive sync: when reference data loads, map names → IDs ────────
  private readonly syncEffect = effect(() => {
    this.state.authors();
    this.state.categories();
    this.syncFormWithBook();
  });

  // ── form ─────────────────────────────────────────────────────────────
  form = this.fb.group({
    isbn: [
      '',
      [Validators.required, Validators.maxLength(20), Validators.pattern(/^[0-9\-X]+$/)],
      [this.isbnAsyncValidator()],
    ],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    publicationYear: [this.currentYear.toString(), [Validators.required, this.yearValidator()]],
    description: ['', [Validators.maxLength(this.maxDescLen)]],
    authorIds: [[] as number[], [this.minSelectionValidator(1)]],
    categoryIds: [[] as number[], [this.minSelectionValidator(1)]],
  });

  // ── lifecycle ────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.state.loadReferenceData();

    this.route.paramMap
      .pipe(
        map((params) => Number(params.get('id'))),
        filter((id) => id > 0),
        switchMap((id) => {
          this.bookId = id;
          return this.bookService.getById(id);
        }),
        takeUntil(this.destroy$),
      )
      .subscribe({
        next: (book) => {
          this.originalIsbn = book.isbn;
          this.isLoadingBook = false;
          this.bookRaw = book;

          // The `syncEffect` will patch the form once reference data arrives.
          // In the meantime, patch known scalar fields now.
          this.form.patchValue({
            isbn: book.isbn,
            title: book.title,
            publicationYear: book.publicationYear.toString(),
            description: book.description ?? '',
          });

          this.cdr.markForCheck();
          setTimeout(() => this.focusFirstErrorOrTitle(), 100);
        },
        error: (err) => {
          this.isLoadingBook = false;
          this.loadError =
            err.status === 404 ? 'Book not found.' : 'Could not load book. Please try again.';
          this.cdr.markForCheck();
        },
      });
  }

  ngOnDestroy(): void {
    if (this.toastTimeout) clearTimeout(this.toastTimeout);
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ── custom validators ─────────────────────────────────────────────────
  private minSelectionValidator(min: number) {
    return (control: AbstractControl): ValidationErrors | null => {
      const value: number[] = control.value ?? [];
      return value.length >= min ? null : { minSelection: { required: min, actual: value.length } };
    };
  }

  private yearValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const val = control.value?.trim();
      if (!val) return { required: true };
      const num = Number(val);
      if (isNaN(num) || !Number.isInteger(num)) return { yearInvalid: 'Must be a whole number' };
      if (num < 1000 || num > this.currentYear)
        return { range: `Must be between 1000 and ${this.currentYear}` };
      return null;
    };
  }

  private isbnAsyncValidator(): AsyncValidatorFn {
    return (control: AbstractControl): Observable<ValidationErrors | null> => {
      const value = control.value?.trim();
      if (!value || value === this.originalIsbn) return of(null);

      return this.bookService.checkIsbnAvailability(value).pipe(
        debounceTime(400),
        map((available) => (available ? null : { isbnTaken: true })),
        catchError(() => of(null)),
      );
    };
  }

  // ── chip helpers ──────────────────────────────────────────────────────
  toggleAuthor(id: number): void {
    this.toggleId('authorIds', id);
  }
  toggleCategory(id: number): void {
    this.toggleId('categoryIds', id);
  }
  isAuthorSelected(id: number): boolean {
    return this.isSelected('authorIds', id);
  }
  isCategorySelected(id: number): boolean {
    return this.isSelected('categoryIds', id);
  }

  private toggleId(field: string, id: number): void {
    const ctrl = this.form.get(field)!;
    const current = (ctrl.value as number[]) ?? [];
    ctrl.setValue(current.includes(id) ? current.filter((x) => x !== id) : [...current, id]);
    ctrl.markAsTouched();
  }

  private isSelected(field: string, id: number): boolean {
    return ((this.form.get(field)?.value as number[]) ?? []).includes(id);
  }

  // ── name helpers ──────────────────────────────────────────────────────
  getAuthorName(authorId: number): string {
    const author = this.state.authors().find(a => a.authorId === authorId);
    return author ? author.fullName : 'Unknown Author';
  }

  getCategoryName(categoryId: number): string {
    const category = this.state.categories().find(c => c.categoryId === categoryId);
    return category ? category.name : 'Unknown Category';
  }

  // ── suggestion dropdown ───────────────────────────────────────────────
  hideAuthorSuggestions(): void {
    setTimeout(() => { this.showAuthorSuggestions = false; }, 200);
  }

  hideCategorySuggestions(): void {
    setTimeout(() => { this.showCategorySuggestions = false; }, 200);
  }

  filterAuthors(event: any): void {
    const term = event.target.value.toLowerCase();
    this.searchAuthorTerm = term;
    this.showAuthorSuggestions = true;
    this.filteredAuthors = term
      ? this.state.authors().filter(a => a.fullName.toLowerCase().includes(term))
      : this.state.authors();
  }

  filterCategories(event: any): void {
    const term = event.target.value.toLowerCase();
    this.searchCategoryTerm = term;
    this.showCategorySuggestions = true;
    this.filteredCategories = term
      ? this.state.categories().filter(c => c.name.toLowerCase().includes(term))
      : this.state.categories();
  }

  clearUpdateError(): void {
    // @ts-ignore - clear the update error signal
    this.state.updateError.set(null);
  }

  // ── error helpers ─────────────────────────────────────────────────────
  getError(field: string): string | null {
    const ctrl = this.form.get(field);
    if (!ctrl || ctrl.valid || (!ctrl.touched && !ctrl.dirty)) return null;
    const e = ctrl.errors;
    if (!e) return null;
    if (e['required']) return 'This field is required.';
    if (e['maxlength']) return `Maximum ${e['maxlength'].requiredLength} characters.`;
    if (e['min']) return `Minimum value is ${e['min'].min}.`;
    if (e['max']) return `Maximum value is ${e['max'].max}.`;
    if (e['pattern']) return 'Digits, hyphens, and X only.';
    if (e['minSelection']) return `Select at least ${e['minSelection'].required}.`;
    if (e['isbnTaken']) return 'This ISBN is used by another book.';
    if (e['yearInvalid']) return e['yearInvalid'];
    if (e['range']) return e['range'];
    return 'Invalid value.';
  }

  get isbnChecking(): boolean {
    return this.form.get('isbn')?.status === 'PENDING';
  }

  isFieldValid(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!ctrl && ctrl.valid && (ctrl.touched || ctrl.dirty);
  }

  get descriptionLength(): number {
    return this.form.get('description')?.value?.length ?? 0;
  }

  showChipGroupError(field: string): boolean {
    const control = this.form.get(field);
    return !!this.getError(field) && !!(control?.touched || control?.dirty);
  }

  // ── reset & dirty detection ────────────────────────────────────────────
  isFormChanged(): boolean {
    if (!this.originalFormValues) return false;
    const current = this.form.getRawValue();
    // Compare arrays (authorIds, categoryIds) properly
    return JSON.stringify(current) !== JSON.stringify(this.originalFormValues);
  }

  resetForm(): void {
    if (!this.isFormChanged()) return;
    if (confirm('Discard all changes and restore original values?')) {
      this.form.reset(this.originalFormValues);
      Object.keys(this.form.controls).forEach((key) => {
        this.form.get(key)?.markAsPristine();
        this.form.get(key)?.markAsUntouched();
      });
      this.cdr.markForCheck();
    }
  }

  // ── sync form with raw book data (names → IDs) ───────────────────────
  private syncFormWithBook(): void {
    if (!this.bookRaw) return;
    const authors = this.state.authors();
    const categories = this.state.categories();
    if (!authors.length || !categories.length) return;

    const authorIds = this.bookRaw.authors
      .map((name) => authors.find((a) => a.fullName === name)?.authorId)
      .filter((id): id is number => id != null);

    const categoryIds = this.bookRaw.categories
      .map((name) => categories.find((c) => c.name === name)?.categoryId)
      .filter((id): id is number => id != null);

    this.form.patchValue({ authorIds, categoryIds });
    this.originalFormValues = this.form.getRawValue();

    // Populate suggestion dropdowns with full lists
    this.filteredAuthors = authors;
    this.filteredCategories = categories;

    this.cdr.markForCheck();
  }

  // ── focus management ──────────────────────────────────────────────────
  private focusFirstErrorOrTitle(): void {
    const firstError = document.querySelector('.input-error, .chip-grid.invalid-group');
    if (firstError) {
      (firstError as HTMLElement).focus();
    } else if (this.pageTitle) {
      this.pageTitle.nativeElement.focus();
    }
  }

  // ── toast ──────────────────────────────────────────────────────────────
  private showToast(message: string, duration = 3000): void {
    this.toastMessage = message;
    if (this.toastTimeout) clearTimeout(this.toastTimeout);
    this.toastTimeout = setTimeout(() => {
      this.toastMessage = null;
      this.cdr.markForCheck();
    }, duration);
  }

  // ── submission (with retry) ────────────────────────────────────────────
  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.form.pending) return;

    const raw = this.form.value;
    const command: UpdateBookCommand = {
      isbn: raw.isbn!.trim(),
      title: raw.title!.trim(),
      publicationYear: parseInt(raw.publicationYear!, 10),
      description: raw.description?.trim() || null,
      authorIds: raw.authorIds ?? [],
      categoryIds: raw.categoryIds ?? [],
    };

    this.state.updateBook(this.bookId, command).subscribe({
      next: (updated) => {
        this.showToast('Book updated successfully');
        this.router.navigate(['/books', updated.bookId]);
      },
      error: () => {
        // error already shown in state.updateError()
        // scroll to API error box
        document
          .querySelector('.api-error-box')
          ?.scrollIntoView({ behavior: 'smooth', block: 'center' });
      },
    });
  }

  retrySubmit(): void {
    if (!this.state.updateError()) return;
    // clear the error state (optional - depends on your state service)
    // then re-submit
    this.onSubmit();
  }

  // ── cancel with unsaved changes guard ─────────────────────────────────
  onCancel(): void {
    if (this.isFormChanged() && !confirm('You have unsaved changes. Leave anyway?')) return;
    this.router.navigate(['/books', this.bookId]);
  }

  // ── browser/refresh guard ─────────────────────────────────────────────
  @HostListener('window:beforeunload', ['$event'])
  unloadGuard($event: BeforeUnloadEvent): void {
    if (this.isFormChanged()) {
      $event.preventDefault();
      $event.returnValue = '';
    }
  }
}
