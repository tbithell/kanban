# DTO Shapes: Kanban Core

**Phase**: 1 | **Branch**: `002-kanban-core`

All DTOs live in `Kanban.Contracts`. Transforms live in `Kanban.Business/Transforms/`.

---

## Request DTOs

```csharp
// CreateBoardRequest.cs
public sealed record CreateBoardRequest(
    [Required, MaxLength(200)] string Name
);

// CreateLaneRequest.cs
public sealed record CreateLaneRequest(
    [Required, MaxLength(100)] string Name
);

// RenameLaneRequest.cs
public sealed record RenameLaneRequest(
    [Required, MaxLength(100)] string Name
);

// MoveLaneRequest.cs
public sealed record MoveLaneRequest(
    [Range(1, int.MaxValue)] int TargetPosition,
    [Range(1, int.MaxValue)] int ExpectedVersion
);

// CreateCardRequest.cs
public sealed record CreateCardRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string? Description = null,
    DateOnly? DueDate = null
);

// UpdateCardRequest.cs
// All fields optional — null means "clear this field"; absent means "do not change"
// Use a JsonPatch-style wrapper so absence is distinguishable from explicit null
public sealed record UpdateCardRequest(
    [MaxLength(200)] string? Title,
    string? Description,          // null = clear; absent = leave unchanged
    bool ClearDueDate,            // true = set to null; false = use DueDate
    DateOnly? DueDate
);

// MoveCardRequest.cs
public sealed record MoveCardRequest(
    [Required] Guid TargetLaneId,
    [Range(1, int.MaxValue)] int TargetPosition,
    [Range(1, int.MaxValue)] int ExpectedVersion
);

// InviteBoardMemberRequest.cs
public sealed record InviteBoardMemberRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required] BoardRoleDto Role
);

// ChangeMemberRoleRequest.cs
public sealed record ChangeMemberRoleRequest(
    [Required] BoardRoleDto Role
);
```

---

## Response DTOs

```csharp
// BoardSummaryDto.cs
public sealed record BoardSummaryDto(
    Guid Id,
    string Name,
    int LaneCount,
    int CardCount,
    BoardRoleDto CallerRole  // caller's own role on this board
);

// BoardDetailDto.cs
public sealed record BoardDetailDto(
    Guid Id,
    string Name,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    BoardRoleDto CallerRole,
    IReadOnlyList<LaneDto> Lanes
);

// LaneDto.cs
public sealed record LaneDto(
    Guid Id,
    Guid BoardId,
    string Name,
    int Position,
    int Version,
    IReadOnlyList<CardDto> Cards
);

// CardDto.cs
public sealed record CardDto(
    Guid Id,
    Guid LaneId,
    Guid BoardId,
    string Title,
    string? Description,
    DateOnly? DueDate,
    int Position,
    int Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt
);

// BoardMemberDto.cs
public sealed record BoardMemberDto(
    Guid UserId,
    string DisplayName,
    BoardRoleDto Role,
    DateTimeOffset JoinedAt
);

// BoardRoleDto.cs (enum, used in request and response DTOs)
public enum BoardRoleDto { Owner, Member, Viewer }

// AcceptInviteResponseDto.cs (extends/replaces the 001 accept response)
public sealed record AcceptInviteResponseDto(
    CurrentUserDto User,
    Guid? BoardId    // non-null if the invitation was board-scoped; frontend redirects here
);
```

---

## Validation Rules (FluentValidation — Kanban.Api/Validators/)

| Validator | Rules |
|-----------|-------|
| `CreateBoardRequestValidator` | Name: not empty, max 200 chars |
| `CreateLaneRequestValidator` | Name: not empty, max 100 chars |
| `RenameLaneRequestValidator` | Name: not empty, max 100 chars |
| `MoveLaneRequestValidator` | TargetPosition ≥ 1, ExpectedVersion ≥ 1 |
| `CreateCardRequestValidator` | Title: not empty, max 200 chars; Description: max 2000 chars if present |
| `UpdateCardRequestValidator` | Title (if provided): not empty, max 200 chars; Description: max 2000 chars if present |
| `MoveCardRequestValidator` | TargetLaneId: not default; TargetPosition ≥ 1; ExpectedVersion ≥ 1 |
| `InviteBoardMemberRequestValidator` | Email: valid format, max 254 chars; Role: valid enum value |
| `ChangeMemberRoleRequestValidator` | Role: valid enum value |

---

## Transform Locations

| Transform class | Transforms |
|----------------|-----------|
| `BoardTransforms` | `Board → BoardSummaryDto`, `Board + Lanes + CallerRole → BoardDetailDto` |
| `LaneTransforms` | `Lane + Cards → LaneDto` |
| `CardTransforms` | `Card → CardDto` |
| `BoardMemberTransforms` | `BoardMember + User → BoardMemberDto` |

All transforms are static methods in the respective classes within `Kanban.Business/Transforms/`.
