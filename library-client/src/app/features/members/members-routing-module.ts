import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MemberListComponent } from './member-list/member-list.component';
import { AddMemberComponent } from './add-member/add-member.component';
import { MemberEditComponent } from './member-edit/member-edit.component';

const routes: Routes = [
  { path: '', component: MemberListComponent },
  { path: 'add', component: AddMemberComponent },
  { path: ':id/edit', component: MemberEditComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class MembersRoutingModule {}
