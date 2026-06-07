import { Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MemberService } from '../services/member.service';
import { ToastService } from '../services/toast.service';
import { MemberDto, CreateMemberCommand, UpdateMemberCommand } from '../models/member.models';

@Injectable({ providedIn: 'root' })
export class MemberStateService {
  readonly members      = signal<MemberDto[]>([]);
  readonly isLoading    = signal(false);
  readonly isCreating   = signal(false);
  readonly isUpdating   = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly createError  = signal<string | null>(null);
  readonly updateError  = signal<string | null>(null);

  constructor(
    private memberService: MemberService,
    private toastService: ToastService,
    private router: Router,
  ) {}

  loadAll(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.memberService.getAll().pipe(
      finalize(() => this.isLoading.set(false)),
    ).subscribe({
      next: members => this.members.set(members),
      error: err => this.errorMessage.set(err.error?.title ?? err.error?.detail ?? 'Failed to load members'),
    });
  }

  create(command: CreateMemberCommand): void {
    this.isCreating.set(true);
    this.createError.set(null);

    this.memberService.create(command).pipe(
      finalize(() => this.isCreating.set(false)),
    ).subscribe({
      next: result => {
        this.toastService.success(`Member #${result.memberId} created`);
        this.router.navigate(['/members']);
      },
      error: err => {
        this.createError.set(err.error?.title ?? err.error?.detail ?? 'Failed to create member');
      },
    });
  }

  update(id: number, command: UpdateMemberCommand): void {
    this.isUpdating.set(true);
    this.updateError.set(null);

    this.memberService.update(id, command).pipe(
      finalize(() => this.isUpdating.set(false)),
    ).subscribe({
      next: () => {
        this.toastService.success('Member updated');
        this.router.navigate(['/members']);
      },
      error: err => {
        this.updateError.set(err.error?.title ?? err.error?.detail ?? 'Failed to update member');
      },
    });
  }

  clearError(): void {
    this.errorMessage.set(null);
    this.createError.set(null);
    this.updateError.set(null);
  }
}
