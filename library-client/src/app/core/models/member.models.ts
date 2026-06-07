export interface MemberDto {
  memberId: number;
  fullName: string;
  email: string;
  memberSince: string;
  status: string;
}

export interface CreateMemberCommand {
  fullName: string;
  email: string;
}

export interface CreateMemberResult {
  memberId: number;
}

export interface UpdateMemberCommand {
  fullName: string;
  email: string;
  status: string;
}
