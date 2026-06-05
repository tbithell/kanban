# Feature Specification: Kanban Core — Board, Lane, Card, and Membership Management

**Feature Branch**: `002-kanban-core`

**Created**: 2026-06-01

**Status**: Draft

**Input**: User description: "The rest of the Kanban application beyond auth/onboarding (already specced as 001-auth-onboarding). Need to spec: board management, lane management, card management, board membership/invites, drag-and-drop reordering. The repo structure and constitution are already established."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Admin Creates a Board and Adds Lanes (Priority: P1)

The administrator — the first user in the system — creates a new board to organize work. They give the board a name and then add an initial set of ordered columns (lanes), such as "To Do," "In Progress," and "Done." Creating a board automatically makes the admin its owner. Until at least one board with lanes exists, there is nothing for members to collaborate on.

**Why this priority**: A board with at least one lane is the prerequisite for every other feature in this spec. Nothing else works until a board exists.

**Independent Test**: Sign in as admin, create a board with a name, add three lanes to it in order, retrieve the board, and confirm all three lanes appear in the correct order with the correct names. Confirm the admin is listed as the board's owner.

**Acceptance Scenarios**:

1. **Given** the admin is signed in, **When** they submit a valid board name, **Then** the system creates the board, assigns the admin as its owner, and returns a summary including the new board's identifier and name.
2. **Given** the admin has created a board, **When** they add a lane with a name to that board, **Then** the lane is appended at the end of the lane list with the next available position and the board's lane list reflects the addition immediately.
3. **Given** a board has three lanes named "To Do," "In Progress," and "Done," **When** a member retrieves the board, **Then** the lanes appear in the order they were created.
4. **Given** the admin is signed in, **When** they attempt to create a board with an empty name or a name exceeding 200 characters, **Then** the system rejects the request with a clear validation message and no board is created.
5. **Given** a registered user who is not an admin, **When** they attempt to create a board, **Then** the request is refused with a permission-denied response.
6. **Given** the admin already has a board named "Sprint Board," **When** they attempt to create another board with the same name, **Then** the system rejects the request with a clear message that a board with that name already exists.

---

### User Story 2 - Board Member Adds and Manages Cards (Priority: P1)

A board member (owner or member role) adds work-item cards to lanes. Each card has at minimum a title; optionally a description and a due date. Members can edit an existing card's title, description, and due date. Members can also delete cards that are no longer needed. Viewers can see all cards but cannot create, edit, or delete them.

**Why this priority**: Cards are the atomic unit of work in a Kanban board — the product delivers zero value without them.

**Independent Test**: Sign in as a board member, add a card with a title and description to a lane, retrieve the lane's cards and confirm the card appears. Edit the card's title, confirm the change persists. Delete the card, confirm it no longer appears and the remaining cards' positions are gapless.

**Acceptance Scenarios**:

1. **Given** a board member is viewing a lane, **When** they submit a new card with a title, **Then** the card is added to the bottom of the lane's card list and the response includes the card's identifier and its assigned position.
2. **Given** a card exists in a lane, **When** a board member submits an update with a new title, description, and due date, **Then** all three values are persisted and subsequent retrieval reflects the updated values.
3. **Given** a card has a due date set, **When** a board member submits an update clearing the due date, **Then** the due date is removed and subsequent retrieval shows no due date on that card.
4. **Given** a card exists, **When** a board member deletes it, **Then** the card is removed from the lane and the remaining cards in that lane have their positions adjusted to remain gapless.
5. **Given** a board viewer (not owner or member), **When** they attempt to create, update, or delete a card, **Then** the request is refused with a permission-denied response.
6. **Given** a member submits a card with an empty title or a title exceeding 200 characters, **When** the system validates the request, **Then** it is rejected with a clear message and no card is created or modified.

---

### User Story 3 - Member Reorders Cards and Lanes via Drag-and-Drop (Priority: P1)

A board member rearranges the board visually. They can drag a card to a new position within its current lane (changing its order among sibling cards), drag a card to a different lane (moving the work item to a new stage), and drag a lane to a new position (reordering the columns). Keyboard users can accomplish the same moves without a mouse. Changes are applied immediately in the UI and persisted so that anyone who loads the board next sees the updated order.

**Why this priority**: Reordering is the primary daily interaction for Kanban users — it represents moving work through the workflow. A board where everything is stuck in a fixed order is not a kanban board.

**Independent Test**: Load a board with two lanes and at least three cards in the first lane. Drag the bottom card to the top of the first lane; confirm the order changes immediately. Drag the first card from the first lane to the second lane; confirm it appears there. Drag the second lane to the first position; confirm the column order changes. Reload the board and confirm all reorderings persisted. Repeat all moves using keyboard controls only.

**Acceptance Scenarios**:

1. **Given** a lane has cards in positions 1, 2, 3, **When** a board member moves the card at position 3 to position 1, **Then** the moved card occupies position 1, the former positions 1 and 2 shift to 2 and 3 respectively, and the new order is persisted.
2. **Given** a card exists in Lane A at position 2 of 3, **When** a board member moves it to Lane B at position 1 of 2, **Then** the card is removed from Lane A (remaining cards' positions adjust), the card appears in Lane B at position 1 (existing Lane B cards shift down), and the change is persisted.
3. **Given** a board has three lanes, **When** a board member drags the third lane to the first position, **Then** the lane ordering updates so the moved lane is first, the other lanes shift right, and the order persists on reload.
4. **Given** a board member is using keyboard navigation, **When** they use the keyboard drag interface to move a card or lane, **Then** the same positional changes and persistence rules apply as for pointer-based drag.
5. **Given** a board viewer, **When** they attempt to drag any card or lane, **Then** the drag is not initiated and no reordering occurs.
6. **Given** two members submit moves for the same card simultaneously, **When** both requests reach the system, **Then** exactly one succeeds and the other receives a conflict response prompting a refresh; the final board state is consistent with exactly one winner.

---

### User Story 4 - Board Owner Invites and Manages Board Members (Priority: P2)

A board owner (or system admin) invites a person to collaborate on a specific board by specifying the person's email address and the role they should have: Owner, Member, or Viewer. The system issues a single-use time-limited invitation link. When the invitee accepts, they become a registered user of the system (if they are not already one) and are immediately added to the board with the assigned role. The owner can also change a member's role or remove a member from the board after they have joined.

**Why this priority**: Collaboration is the entire point of a Kanban board — a solo board is a to-do list. But the core board experience from US1–US3 can be demonstrated solo first, making this P2.

**Independent Test**: Sign in as a board owner, issue a board invitation for a new email with the Member role. Accept the invitation with a matching Google account. Confirm the new user appears in the board's member list with the Member role and can add cards. Change their role to Viewer and confirm they can no longer add cards. Remove them from the board and confirm they no longer appear in the member list and receive a not-found response when accessing the board.

**Acceptance Scenarios**:

1. **Given** a board owner is signed in, **When** they invite `newperson@example.com` to a board with the Member role, **Then** the system records a pending board membership for that email and role and returns a redemption link the owner can share.
2. **Given** `newperson@example.com` has not yet registered, **When** they open the board invitation link and complete sign-in with a matching account, **Then** the system creates a registered user record for them, adds them to the board with the Member role, and presents the board they were invited to.
3. **Given** `existing@example.com` is already a registered user, **When** they open a board invitation and complete sign-in, **Then** the system adds them to the board with the specified role without creating a duplicate user record.
4. **Given** a board has a member, **When** the board owner changes that member's role from Member to Viewer, **Then** the member's board role is updated and they can no longer perform write operations on the board; their existing read access continues.
5. **Given** a board has a member, **When** the board owner removes that member, **Then** the member no longer appears in the board's member list and any subsequent request from that user to access the board returns a not-found response.
6. **Given** a registered member is the sole Owner of a board, **When** someone attempts to remove them or change their role to a non-Owner role, **Then** the system refuses with a clear message that at least one Owner must remain on the board.
7. **Given** a board Member (not Owner or Admin), **When** they attempt to invite someone, change a member's role, or remove a member, **Then** the request is refused with a permission-denied response.

---

### User Story 5 - Viewer Browses a Board Read-Only (Priority: P3)

A registered user assigned the Viewer role on a board can see the board's lanes and cards in full but cannot make any changes. This enables stakeholders to monitor progress without the risk of accidental edits.

**Why this priority**: A pure read-only persona is valuable for reporting and oversight, but the product delivers its primary value without it — owners and members can share a board without assigning Viewer-only roles.

**Independent Test**: Assign a user the Viewer role on a board. Sign in as that user. Confirm all lanes and cards are visible. Confirm that any attempt to create, update, delete, or move any item is refused.

**Acceptance Scenarios**:

1. **Given** a user has the Viewer role on a board, **When** they retrieve the board, **Then** all lanes and cards are returned in full.
2. **Given** a user has the Viewer role, **When** they attempt any write operation — create card, update card, delete card, create lane, rename lane, delete lane, move card, reorder lane — **Then** each request is refused with a permission-denied response.

---

### User Story 6 - Unify Input Validation Using FluentValidation Across All Layers (Priority: P1)

The codebase currently maintains two separate input-validation mechanisms: FluentValidation at the API boundary (where it handles user-supplied input on incoming requests) and a bespoke `Verify` guard-clause class in the inner layers (Business, DataAccess, Domain, AntiCorruption). This duplication means two patterns to learn, two exception types to map in error-handling middleware, and two sets of test assertions to maintain. Consolidating on FluentValidation throughout removes this inconsistency and establishes a single, well-known library as the project's universal input-validation tool.

**Why this priority**: All new services introduced in US1–US5 must adopt the chosen validation pattern from day one. Completing this migration before implementing the board, lane, card, and membership services prevents the legacy pattern from spreading further and avoids a double-migration.

**Independent Test**: Call any business service method or repository method with a null, empty, or otherwise invalid argument. Confirm a `FluentValidation.ValidationException` is thrown (not `ArgumentNullException` or `ArgumentException`). Confirm that the API returns a `422 Unprocessable Entity` with a Problem Details body describing the validation failure. Confirm no reference to `Verify.cs` remains in the compiled solution.

**Acceptance Scenarios**:

1. **Given** a business service method is called with a null required argument, **When** the call is processed, **Then** a `FluentValidation.ValidationException` is thrown describing the null violation — no `ArgumentNullException` is raised.
2. **Given** a business service method is called with an empty string where a non-empty value is required, **When** the call is processed, **Then** a `FluentValidation.ValidationException` is thrown describing the empty-string violation.
3. **Given** a repository method is called with a default (empty) GUID where a valid identifier is required, **When** the call is processed, **Then** a `FluentValidation.ValidationException` is thrown describing the invalid identifier.
4. **Given** any inner-layer `ValidationException` propagates to the API, **When** the exception handler processes it, **Then** the response status is `422 Unprocessable Entity` with a Problem Details body containing a `code` field and the validation error details — no stack trace is exposed.
5. **Given** the migration is complete, **When** the solution is compiled, **Then** there are zero references to `Verify.cs`, `ParameterVerifier<T>`, or any of the `Verify` extension classes — they do not exist in the compiled output.

---

### Edge Cases

- **Concurrent card moves to the same position**: two members drag different cards to the same lane position simultaneously; both operations succeed but land at distinct adjacent positions — neither card is lost.
- **Delete a lane that contains cards**: all cards within the lane are removed along with the lane in a single atomic operation; the UI presents a destructive confirmation step before executing.
- **Reorder when only one card or lane exists**: moving the sole item in a list is a no-op that succeeds without error.
- **Board with no lanes**: retrieving such a board returns an empty lane list, not an error.
- **Card position gaps after deletion**: removing a card from the middle of a list produces a gapless position sequence; retrieval must never expose gaps.
- **Non-member accesses a board by ID**: a registered user who is not a member of the requested board receives a not-found response — never a forbidden response — to prevent board enumeration.
- **Admin access boundary**: an admin must be a board member to access board data. Creating a board automatically grants the admin the Owner board role on that board; they have no ambient access to boards they did not create or join.
- **Renaming a lane to a name that duplicates an existing lane in the same board**: the system rejects with a clear message that lane names must be unique within a board.
- **Board deletion**: only the board Owner or system Admin can delete a board; all lanes, cards, memberships, and card assignments are deleted atomically; this is permanent and the UI requires explicit confirmation.
- **Invitation to an email already on the board**: if an unconsumed, unexpired invitation already exists for that email on that board, the existing link is returned rather than a new one being issued.

## Requirements *(mandatory)*

### Functional Requirements

#### Board Management

- **FR-001**: System MUST allow system administrators to create boards by providing a name (1–200 characters) that is unique across all boards. On creation, the creating administrator is automatically assigned the Owner board role.
- **FR-002**: System MUST list only boards the requesting user is a member of (any role). Boards the user has no membership in MUST NOT appear in any listing.
- **FR-003**: System MUST allow board Owners and system Admins to delete a board. Deletion MUST atomically remove all lanes, cards, memberships, and card assignments associated with that board.
- **FR-004**: System MUST return a not-found response for any board resource request from a registered user who is not a member of that board — regardless of whether the board actually exists.

#### Lane Management

- **FR-005**: System MUST allow board Owners and Members to create lanes within a board by providing a name (1–100 characters) that is unique within that board. A new lane is appended at the end of the current lane list.
- **FR-006**: System MUST allow board Owners and Members to rename an existing lane. The new name MUST be unique within the board.
- **FR-007**: System MUST allow board Owners and Members to delete a lane. Deleting a lane MUST also delete all cards it contains in a single atomic operation.
- **FR-008**: System MUST allow board Owners and Members to move a lane to a new position within its board. All other lanes' positions MUST be updated so the sequence remains gapless after every reorder.
- **FR-009**: Lane positions MUST be unique within a board and MUST remain gapless (1, 2, 3, …) after every create, delete, and reorder operation.

#### Card Management

- **FR-010**: System MUST allow board Owners and Members to create cards within a lane by providing a title (1–200 characters). Description (up to 2000 characters) and due date are optional at creation. A new card is appended at the bottom of the lane's card list.
- **FR-011**: System MUST allow board Owners and Members to update a card's title, description, and due date individually or together. Explicitly clearing the due date (submitting a null value) MUST remove it from the card.
- **FR-012**: System MUST allow board Owners and Members to delete a card. The remaining cards in that lane MUST have their positions adjusted to remain gapless.
- **FR-013**: System MUST allow board Owners and Members to move a card to a new position within its current lane or to a different lane at a specified position. All affected card positions within both lanes MUST be updated atomically.
- **FR-014**: Card positions MUST be unique within a lane and MUST remain gapless after every create, move, and delete operation.

#### Board Membership Management

- **FR-015**: System MUST allow board Owners and system Admins to invite a person to a board by specifying the invitee's email address and the intended board role (Owner, Member, or Viewer).
- **FR-016**: System MUST issue a single-use, time-limited (7-day) redemption link per board invitation. If an unconsumed, unexpired invitation for the same email on the same board already exists, the existing link MUST be returned rather than a new one being created.
- **FR-017**: On invitation acceptance, if the invitee is not yet a registered user, the system MUST create a registered user record for them AND activate their board membership as a single atomic outcome.
- **FR-018**: On invitation acceptance, if the invitee is already a registered user, the system MUST activate their board membership without creating a duplicate user record.
- **FR-019**: System MUST allow board Owners and system Admins to change a board member's role. A board MUST always retain at least one Owner — changing the last Owner's role MUST be refused with a clear error.
- **FR-020**: System MUST allow board Owners and system Admins to remove a member from a board. Removing the last Owner MUST be refused with a clear error.
- **FR-021**: System MUST return the full list of current board members and their roles to any user who is a member of that board.

#### Validation Unification

- **FR-027**: System MUST replace all usages of the `Verify` guard-clause class in Business, DataAccess, Domain, and AntiCorruption layers with FluentValidation `AbstractValidator<T>` subclasses or inline `InlineValidator<T>` instances. After migration, the `Verify.cs` file and all its extension classes MUST be deleted from the solution.
- **FR-028**: When a FluentValidation `ValidationException` is thrown from an inner layer and reaches the API, the system MUST return a `422 Unprocessable Entity` response in RFC 7807 Problem Details format. The response MUST include a `code` field (e.g. `"validation.failed"`) and MUST NOT include stack traces or internal class names.
- **FR-029**: All existing unit and integration tests that currently assert `ArgumentNullException` or `ArgumentException` originating from `Verify` calls MUST be updated to assert `FluentValidation.ValidationException` instead. The test assertions MUST verify that the correct field name and error message appear in the exception's `Errors` collection.

#### Drag-and-Drop Reordering (UI)

- **FR-022**: The UI MUST allow board Owners and Members to reorder cards within a lane and between lanes by dragging them. Board Viewers MUST NOT be able to initiate a drag.
- **FR-023**: The UI MUST allow board Owners and Members to reorder lanes within a board by dragging them. Board Viewers MUST NOT be able to drag lanes.
- **FR-024**: All drag-and-drop reordering operations MUST be fully accessible via keyboard controls that produce equivalent results to pointer-based drag.
- **FR-025**: The UI MUST provide descriptive announcements for screen reader users at each stage of a drag-and-drop operation: item picked up, current position announced as user moves, item dropped or move cancelled.
- **FR-026**: When a drag-and-drop move fails to persist on the server, the UI MUST visually restore the item to its original position and display a persistent, non-auto-dismissing message explaining the failure.

### Key Entities

- **Board**: A named workspace that organizes work into lanes. Created by a system administrator. Unique name, creation date, and an ordered set of lanes. Accessible only to members.
- **Lane**: An ordered column within a Board representing a stage of work. Has a name unique within its board and an integer position that determines left-to-right display order. Deleting a lane deletes all its cards.
- **Card**: A work item within a Lane. Has a title (required), optional description, optional due date, and an integer position within its lane. Can be moved between lanes.
- **BoardMember**: An association between a registered User and a Board with an assigned role (Owner, Member, or Viewer). A board must always have at least one Owner.
- **CardAssignee**: An optional association between a Card and one or more registered Users indicating responsibility for the work item. Assignees must be members of the board.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can create a board, add three lanes, and add a card to each lane in under 60 seconds from first interaction.
- **SC-002**: Dragging a card between lanes reflects the new position in the UI in under 100 milliseconds from release, and the server confirms the move in under 2 seconds.
- **SC-003**: Dragging a lane to a new position reflects the change in the UI in under 100 milliseconds from release, and the server confirms the move in under 2 seconds.
- **SC-004**: A board with 10 lanes and 50 cards per lane loads completely in under 3 seconds on a typical home internet connection.
- **SC-005**: When a card or lane move fails, the item returns visually to its original position with a visible persistent error message within 1 second of the failure — the user is never left with a board state that silently disagrees with the server.
- **SC-006**: Across 20 simultaneous move attempts on the same card, exactly one succeeds and the rest receive a conflict response. The board is in a single consistent state after all attempts resolve.
- **SC-007**: A keyboard-only user can move a card between lanes and reorder lanes without using a pointing device. All interactions are reachable by keyboard tab order.
- **SC-008**: A board viewer can load and read a board with 100 cards in under 3 seconds and cannot trigger any write operation regardless of how they interact with the UI.
- **SC-009**: Board invitation acceptance by a person not yet registered completes and presents the invited board in under 2 minutes from opening the link (assumes the invitee has a valid account with the identity provider).
- **SC-010**: Requests for a board by a non-member return a not-found response in under 200 milliseconds — board existence is never revealed to non-members.
- **SC-011**: After migration, the solution contains zero references to the `Verify` class. Any invalid argument passed to a Business, DataAccess, Domain, or AntiCorruption public method produces a `FluentValidation.ValidationException` — never an `ArgumentNullException` or `ArgumentException`.

## Assumptions

- **Board names are unique system-wide**: since only system administrators can create boards, all boards share one naming namespace; two boards may not have the same name regardless of who manages them.
- **Lane names are unique within a board**: two lanes in the same board may not share a name; two lanes on different boards may.
- **Card titles are not unique**: multiple cards within the same lane may have identical titles; no uniqueness constraint is enforced at the card level.
- **Card assignees must be board members**: only users who are current members of the board may be assigned to cards on that board. Full card assignment management (assigning, unassigning) is part of the card management feature but may be deferred to a follow-up increment if scope demands it.
- **Board deletion is permanent**: there is no trash or recovery mechanism. The UI requires explicit confirmation before executing.
- **Lane deletion cascades to all cards within that lane**: this is a destructive operation; the UI warns the user of the impact before proceeding.
- **The invitation acceptance flow from the auth spec (001-auth-onboarding) is extended for board invitations**: board invitations reuse the same token and acceptance mechanism but additionally create the board membership record on acceptance. No separate invitation infrastructure is introduced.
- **No email notifications are sent**: board invitation links are returned to the inviting Owner in the UI for out-of-band sharing, consistent with the approach established in the auth spec.
- **Optimistic updates in the UI for drag-and-drop**: moves apply immediately in the UI before server confirmation. A failed server response reverses the visual change and notifies the user with a persistent message.
- **Card assignment details are scoped to this feature's data model but full assignee management UI may ship as a follow-on**: the CardAssignee entity and its persistence rules are defined here; the full UI for assigning and unassigning users to cards may follow in a subsequent increment.
