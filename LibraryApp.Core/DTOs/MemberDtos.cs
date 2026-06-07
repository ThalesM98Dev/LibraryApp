namespace LibraryApp.Core.DTOs;

public record CreateMemberCommand(
    string FullName,
    string Email
);

public record UpdateMemberCommand(
    string? FullName,
    string? Email,
    string? Status
);

public record MemberDto(
    int MemberId,
    string FullName,
    string Email,
    DateTime MemberSince,
    string Status
);

public record CreateMemberResult(
    int MemberId,
    string FullName,
    string Email
);

public record TrackedMemberDto(
    int MemberId,
    string CurrentFullName,
    string CurrentEmail,
    string CurrentStatus
);
