import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, computed } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Book, PagedResult } from '../../../core/models/book.models';
import { BookStateService } from '../../../core/state/book-state.service';

@Component({
  selector: 'app-book-search',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './book-search.component.html',
  styleUrl: './book-search.component.scss',
})
export class BookSearchComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly state = inject(BookStateService);

  readonly pageSize = 20;
  readonly categories = computed(() => this.state.categories());

  form = this.fb.group({
    title: [''],
    authorName: [''],
    categoryId: [null as number | null],
  });

  ngOnInit(): void {
    this.state.loadReferenceData();
    this.search(1);
  }

  onSubmit(): void {
    this.form.markAllAsTouched();
    if (this.form.valid) {
      this.search(1);
    }
  }

  onPageChange(page: number): void {
    this.search(page);
  }

  trackByBookId(_: number, book: Book): number {
    return book.bookId;
  }

  totalPages(result: PagedResult<Book>): number {
    return Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  }

  pageNumbers(current: number, total: number): (number | '...')[] {
    const pages: (number | '...')[] = [];
    if (total <= 7) {
      for (let i = 1; i <= total; i++) pages.push(i);
      return pages;
    }
    pages.push(1);
    if (current > 3) pages.push('...');
    for (let i = Math.max(2, current - 1); i <= Math.min(total - 1, current + 1); i++) {
      pages.push(i);
    }
    if (current < total - 2) pages.push('...');
    pages.push(total);
    return pages;
  }

  bookInitial(title: string): string {
    return title.charAt(0).toUpperCase();
  }

  search(page: number): void {
    const { title, authorName, categoryId } = this.form.getRawValue();
    this.state.search({
      title: title?.trim() || undefined,
      authorName: authorName?.trim() || undefined,
      categoryId: categoryId ?? undefined,
      page,
      pageSize: this.pageSize,
    });
  }
}
