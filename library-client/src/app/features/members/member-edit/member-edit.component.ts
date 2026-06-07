import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Subscription } from 'rxjs';
import { MemberStateService } from '../../../core/state/member-state.service';
import { MemberService } from '../../../core/services/member.service';
import { MemberDto } from '../../../core/models/member.models';

@Component({
  selector: 'app-member-edit',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './member-edit.component.html',
  styleUrls: ['./member-edit.component.scss'],
})
export class MemberEditComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly memberService = inject(MemberService);
  readonly state = inject(MemberStateService);

  readonly isLoadingMember = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly member = signal<MemberDto | null>(null);
  private sub: Subscription | null = null;

  form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    status: ['Active', Validators.required],
  });

  ngOnInit(): void {
    const rawId = this.route.snapshot.paramMap.get('id');
    const id = Number(rawId);
    if (!id) {
      this.loadError.set(`Invalid member ID: '${rawId}'`);
      return;
    }

    this.isLoadingMember.set(true);
    this.loadError.set(null);
    this.sub = this.memberService.getById(id).subscribe({
      next: member => {
        this.member.set(member);
        this.form.patchValue({
          fullName: member.fullName ?? '',
          email: member.email ?? '',
          status: member.status ?? 'Active',
        });
        this.isLoadingMember.set(false);
      },
      error: err => {
        this.isLoadingMember.set(false);
        this.loadError.set(
          err.status
            ? `Failed to load member (HTTP ${err.status})`
            : 'Could not reach the server. Is the API running?',
        );
      },
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  onSubmit(): void {
    const m = this.member();
    if (this.form.invalid || !m) {
      this.form.markAllAsTouched();
      return;
    }
    this.state.update(m.memberId, this.form.value as any);
  }
}
