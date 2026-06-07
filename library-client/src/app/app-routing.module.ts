import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'loans', pathMatch: 'full' },
  { path: 'loans', loadChildren: () =>
      import('./features/loans/loans-module').then(m => m.LoansModule) },
  { path: 'books', loadChildren: () =>
      import('./features/books/books-module').then(m => m.BooksModule) },
  { path: 'members', loadChildren: () =>
      import('./features/members/members-module').then(m => m.MembersModule) }
];