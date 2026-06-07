import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-toast',
  standalone: false,
  templateUrl: './toast.html',
  styleUrl: './toast.scss',
})
export class Toast {
  @Input() title = '';
  @Input() message = '';
  @Input() type: 'success' | 'error' | 'info' | 'warning' = 'info';

  getIconClass(): string {
    return `toast-icon-${this.type}`;
  }

  // For simplicity, we'll use current time - in a real app this would be passed in
  get timestamp() {
    return new Date();
  }

  close() {
    // In a real implementation, this would emit an event to the toast service
    // to remove the toast from display
  }
}
