import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { MembersRoutingModule } from './members-routing-module';
import { MemberListComponent } from './member-list/member-list.component';
import { MemberEditComponent } from './member-edit/member-edit.component';
import { AddMemberComponent } from './add-member/add-member.component';

@NgModule({
  imports: [
    MembersRoutingModule,
    MemberListComponent,
    MemberEditComponent,
    ReactiveFormsModule,
    CommonModule,
  ],
  declarations: [AddMemberComponent],
})
export class MembersModule {}
