import { Component, OnInit, inject, output, ChangeDetectorRef, effect } from '@angular/core';
import { FormBuilder, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { BookStateService } from '../../../core/state/book-state.service';
import { CreateBookCommand } from '../../../core/models/book.models';
import { Router } from '@angular/router';

@Component({
  selector: 'app-add-book',
  templateUrl: './add-book.component.html',
  styleUrls: ['./add-book.component.scss'],
  standalone: false,
})
export class AddBookComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly cdr = inject(ChangeDetectorRef);
  readonly state = inject(BookStateService);

  // Output event so parent can close a modal or navigate away
  bookCreated = output<void>();

  readonly conditions = ['New', 'Good', 'Fair', 'Poor'];
  readonly currentYear = new Date().getFullYear();
  readonly maxDescLen = 4000;
  readonly descriptionWarnThreshold = 3600;
  private readonly router = inject(Router);

  // UI state
  focusedField = '';
  showAuthorSuggestions = false;
  showCategorySuggestions = false;
  searchAuthorTerm = '';
  searchCategoryTerm = '';

  // Search filtering properties
  filteredAuthors: any[] = [];
  filteredCategories: any[] = [];
  searchAuthors = false;
  searchCategories = false;

  // Populate suggestion dropdowns once reference data is available
  private readonly suggestEffect = effect(() => {
    const authors = this.state.authors();
    const categories = this.state.categories();
    if (authors.length && !this.filteredAuthors.length) {
      this.filteredAuthors = authors;
    }
    if (categories.length && !this.filteredCategories.length) {
      this.filteredCategories = categories;
    }
  });

  form = this.fb.group({
    isbn: ['', [Validators.required, Validators.maxLength(20), Validators.pattern(/^[0-9\-X]+$/)]],
    title: ['', [Validators.required, Validators.maxLength(300)]],
    publicationYear: [
      this.currentYear.toString(),
      [Validators.required, this.yearValidator()],
    ],
    description: ['', [Validators.maxLength(this.maxDescLen)]],
    authorIds: [[] as number[], [this.minSelectionValidator(1)]],
    categoryIds: [[] as number[], [this.minSelectionValidator(1)]],
    initialCopies: [1, [Validators.required, Validators.min(1), Validators.max(100)]],
    copyCondition: ['', Validators.required],
  });

  // ── custom validators ─────────────────────────────────────────────────
  private minSelectionValidator(min: number) {
    return (control: AbstractControl): ValidationErrors | null => {
      const value: number[] = control.value ?? [];
      return value.length >= min ? null : { minSelection: { required: min, actual: value.length } };
    };
  }

  private yearValidator() {
    return (control: AbstractControl): ValidationErrors | null => {
      const val = control.value?.toString().trim();
      if (!val) return { required: true };
      const num = Number(val);
      if (isNaN(num) || !Number.isInteger(num)) return { yearInvalid: 'Must be a whole number' };
      if (num < 1000 || num > this.currentYear)
        return { range: `Must be between 1000 and ${this.currentYear}` };
      return null;
    };
  }

  ngOnInit(): void {
    this.state.loadReferenceData();
  }

  // Multi-select helpers — toggle an ID in/out of the array control
  toggleAuthor(id: number): void {
    this.toggle('authorIds', id);
  }
  toggleCategory(id: number): void {
    this.toggle('categoryIds', id);
  }

  isAuthorSelected(id: number): boolean {
    return this.isSelected('authorIds', id);
  }
  isCategorySelected(id: number): boolean {
    return this.isSelected('categoryIds', id);
  }

  private toggle(field: string, id: number): void {
    const ctrl = this.form.get(field)!;
    const current: number[] = ctrl.value ?? [];
    ctrl.setValue(current.includes(id) ? current.filter((x) => x !== id) : [...current, id]);
    ctrl.markAsTouched();
  }

  private isSelected(field: string, id: number): boolean {
    return (this.form.get(field)?.value ?? []).includes(id);
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

  // Helper methods to get names
  getAuthorName(authorId: number): string {
    const author = this.state.authors().find(a => a.authorId === authorId);
    return author ? author.fullName : 'Unknown Author';
  }

  getCategoryName(categoryId: number): string {
    const category = this.state.categories().find(c => c.categoryId === categoryId);
    return category ? category.name : 'Unknown Category';
  }

  // Suggestion dropdown visibility
  hideAuthorSuggestions(): void {
    setTimeout(() => { this.showAuthorSuggestions = false; }, 200);
  }

  hideCategorySuggestions(): void {
    setTimeout(() => { this.showCategorySuggestions = false; }, 200);
  }

  // Search filtering methods
  filterAuthors(event: any): void {
    const searchTerm = event.target.value.toLowerCase();
    this.searchAuthorTerm = searchTerm;
    this.showAuthorSuggestions = true;
    this.searchAuthors = true;
    if (searchTerm) {
      this.filteredAuthors = this.state.authors().filter(author =>
        author.fullName.toLowerCase().includes(searchTerm)
      );
    } else {
      this.filteredAuthors = this.state.authors();
    }
  }

  filterCategories(event: any): void {
    const searchTerm = event.target.value.toLowerCase();
    this.searchCategoryTerm = searchTerm;
    this.showCategorySuggestions = true;
    this.searchCategories = true;
    if (searchTerm) {
      this.filteredCategories = this.state.categories().filter(category =>
        category.name.toLowerCase().includes(searchTerm)
      );
    } else {
      this.filteredCategories = this.state.categories();
    }
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.value;
    const command: CreateBookCommand = {
      isbn: raw.isbn!,
      title: raw.title!,
      publicationYear: Number(raw.publicationYear!),
      description: raw.description || null,
      authorIds: raw.authorIds ?? [],
      categoryIds: raw.categoryIds ?? [],
      initialCopies: raw.initialCopies!,
      copyCondition: raw.copyCondition!,
    };

    this.state.createBook(command).subscribe({
      next: () => {
        this.form.reset({
          publicationYear: this.currentYear.toString(),
          initialCopies: 1,
          copyCondition: '',
        });
        this.filteredAuthors = [];
        this.filteredCategories = [];
        this.searchAuthors = false;
        this.searchCategories = false;
        // Navigate away after a brief delay to allow success message to be seen
        setTimeout(() => {
          this.router.navigate(['/books']); // return to catalogue
          this.bookCreated.emit(); // let parent close/navigate
        }, 1500);
      },
    });
  }

  onSubmitSuccess(): void {
    // Clear the last created book ID to hide success message
    // This is a temporary solution - in a real app we'd have a better success message system
    // @ts-ignore
    this.state.lastCreatedBookId.set(null);
  }

  onCancel(): void {
    this.form.reset({
      publicationYear: this.currentYear.toString(),
      initialCopies: 1,
      copyCondition: '',
    });
    this.filteredAuthors = [];
    this.filteredCategories = [];
    this.searchAuthors = false;
    this.searchCategories = false;
    this.router.navigate(['/books']);
  }
}
