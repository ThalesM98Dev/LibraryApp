import { Injectable, signal } from '@angular/core';

export interface Toast {
  id:      number;
  message: string;
  title:   string;
  type:    'success' | 'error' | 'info' | 'warning';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private counter = 0;
  readonly toasts = signal<Toast[]>([]);

  success(message: string, title: string = 'Success') { this.add(message, 'success', title); }
  error(message: string, title: string = 'Error')     { this.add(message, 'error', title);   }
  info(message: string, title: string = 'Information'){ this.add(message, 'info', title);    }
  warning(message: string, title: string = 'Warning') { this.add(message, 'warning', title); }

  dismiss(id: number) {
    this.toasts.update(list => list.filter(t => t.id !== id));
  }

  private add(message: string, type: Toast['type'], title: string) {
    const id = ++this.counter;
    this.toasts.update(list => [...list, { id, message, title, type }]);
    setTimeout(() => this.dismiss(id), 5000);  // auto-dismiss after 5s
  }
}