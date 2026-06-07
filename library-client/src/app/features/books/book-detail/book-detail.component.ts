import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { Book } from '../../../core/models/book.models';
import { BookService } from '../../../core/services/book.service';

@Component({
  selector: 'app-book-detail',
  imports: [CommonModule, RouterLink],
  templateUrl: './book-detail.component.html',
  styleUrl: './book-detail.component.scss',
})
export class BookDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bookService = inject(BookService);

  readonly book = signal<Book | null>(null);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  private currentId = 0;

  ngOnInit(): void {
    this.loadBook();
  }

  retry(): void {
    this.loadBook();
  }

  bookInitial(title: string): string {
    return title.charAt(0).toUpperCase();
  }

  bookColor(id: number): string {
    return `hsl(${id * 37 % 360}, 48%, 75%)`;
  }

  private loadBook(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.errorMessage.set('Invalid book ID');
      this.isLoading.set(false);
      return;
    }

    this.currentId = id;
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.book.set(null);

    this.bookService.getById(id).pipe(
      finalize(() => this.isLoading.set(false))
    ).subscribe({
      next: book => this.book.set(book),
      error: () => this.errorMessage.set('Book not found'),
    });
  }
}
