import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MemberStateService } from '../../../core/state/member-state.service';

@Component({
  selector: 'app-member-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './member-list.component.html',
  styleUrls: ['./member-list.component.scss'],
})
export class MemberListComponent implements OnInit {
  readonly state = inject(MemberStateService);

  ngOnInit(): void {
    this.state.loadAll();
  }

  memberIdTrack(_: number, member: { memberId: number }): number {
    return member.memberId;
  }

  onDelete(id: number): void {
    if (!confirm('Delete this member? This action cannot be undone.')) return;
    this.state.deleteMember(id);
  }
}
