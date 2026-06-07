import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common'; // Add this import

import { BooksRoutingModule } from './books-routing-module';
import { BookSearchComponent } from './book-search/book-search.component';
import { BookDetailComponent } from './book-detail/book-detail.component';
import { AddBookComponent } from './add-book/add-book.component';
import { ReactiveFormsModule } from '@angular/forms';
import { BookEditComponent } from './book-edit/book-edit.component';

@NgModule({
  imports: [
    BooksRoutingModule,
    BookSearchComponent,
    BookDetailComponent,
    BookEditComponent,
    ReactiveFormsModule,
    CommonModule,
  ], // Add CommonModule
  declarations: [AddBookComponent],
})
export class BooksModule {}
