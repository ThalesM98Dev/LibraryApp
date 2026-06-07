import { NgModule, ErrorHandler, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import {
  HTTP_INTERCEPTORS,
  provideHttpClient,
  withFetch,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { RouterModule } from '@angular/router';
import { App } from './app';
import { routes } from './app-routing.module';
import { SharedModule } from './shared/shared-module';
import { ApiInterceptor } from './core/interceptors/api.interceptor';
import { GlobalErrorHandler } from './core/error/global-error-handler';

@NgModule({
  declarations: [App],
  imports: [BrowserModule, RouterModule.forRoot(routes), SharedModule],
  providers: [
    provideHttpClient(withFetch(), withInterceptorsFromDi()),
    { provide: ErrorHandler, useClass: GlobalErrorHandler },
    { provide: HTTP_INTERCEPTORS, useClass: ApiInterceptor, multi: true },
  ],
  bootstrap: [App],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class AppModule {}