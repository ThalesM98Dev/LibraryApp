export interface Book {
  bookId: number;
  isbn: string;
  title: string;
  publicationYear: number;
  description: string | null;
  availableCopies: number;
  authors: string[];
  categories: string[];
}

export interface BookSearchQuery {
  title?: string;
  authorName?: string;
  categoryId?: number;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface CreateBookCommand {
  isbn: string;
  title: string;
  publicationYear: number;
  description: string | null;
  authorIds: number[];
  categoryIds: number[];
  initialCopies: number;
  copyCondition: string;
}

export interface CreateBookResult {
  bookId: number;
  isbn: string;
  title: string;
  copiesCreated: number;
}

// For populating the dropdowns in the form
export interface AuthorOption {
  authorId: number;
  fullName: string;
}
export interface CategoryOption {
  categoryId: number;
  name: string;
}
// Add alongside existing types

export interface UpdateBookCommand {
  isbn?:            string;
  title?:           string;
  publicationYear?: number;
  description?:     string | null;
  authorIds?:       number[];
  categoryIds?:     number[];
}
