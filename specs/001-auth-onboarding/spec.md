# Feature Specification: Authentication and User Onboarding

**Feature Branch**: `001-auth-onboarding`

**Created**: 2026-05-25

**Status**: Draft

**Input**: User description: "Authentication and user onboarding: Google OAuth login, RegisteredUser policy gate, initial admin seeding, invite token issuance and acceptance"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seeded Admin Signs In For The First Time (Priority: P1)

The system is configured with a designated administrator email at install time. On
first launch, the administrator opens the application, signs in with their Google
account, and is recognized as the system's admin. Without this story working, no
one can ever access the system — it is the bedrock of every subsequent flow.

This story ALSO covers the negative path: anyone with a Google account who is not
the seeded admin (and has not been invited) is denied access. The gate must
distinguish "successfully authenticated by Google but not authorized to use this
system" from generic "sign-in failed" — the user is told they are not registered,
not that their credentials are wrong.

**Why this priority**: Foundation. Until the admin can sign in and the gate refuses
strangers, nothing else in the product is reachable.

**Independent Test**: Configure an admin email, launch the system, sign in with a
Google account matching that email, and confirm admin lands on a "you're signed in"
view. Separately, sign in with a Google account that does NOT match the seeded
email and confirm the user is shown a "not registered" message — not "sign-in
failed" — and is denied access to all application data.

**Acceptance Scenarios**:

1. **Given** the system has been installed with admin email `admin@example.com` configured, **When** a person with that exact Google account email completes Google sign-in for the first time, **Then** they land on a signed-in landing page that identifies them as the administrator.
2. **Given** the admin has signed in before and the system has linked their Google identity to the admin record, **When** the admin signs in again later, **Then** the system recognizes them immediately without re-running the first-time link step.
3. **Given** a person with a Google account whose email is not the seeded admin and who has not been invited, **When** they complete Google sign-in, **Then** they receive a "you are not registered" message and are denied access to any application data or actions.
4. **Given** an unauthenticated user, **When** they attempt to reach any application page or action without signing in, **Then** they are redirected to the Google sign-in flow and not allowed to proceed until authenticated.

---

### User Story 2 - Admin Issues An Invitation (Priority: P1)

The administrator needs to add a new person to the system. The admin enters the
invitee's email, the system generates a time-limited, single-use redemption link,
and the admin can copy that link to share with the invitee out-of-band (chat,
email of the admin's choosing, etc.). The invitation is recorded so the
acceptance flow knows it is valid.

**Why this priority**: Without invitations, only the admin can ever use the
product. Two people make a kanban useful; one person makes it a notepad. Inviting
the second human is the smallest possible step toward a multi-user product.

**Independent Test**: Sign in as admin, enter an invitee email, submit, and
confirm a redemption link is returned that contains a token. Confirm the
invitation exists in the system's record of issued invitations with an
expiration in the future. Confirm the same redemption link is NOT issued again
to that email while it remains unconsumed and unexpired.

**Acceptance Scenarios**:

1. **Given** the admin is signed in, **When** they submit a valid email address for invitation, **Then** the system records an invitation with a 7-day expiration and returns a redemption link that the admin can copy to share with the invitee.
2. **Given** an unconsumed and unexpired invitation already exists for `person@example.com`, **When** the admin attempts to invite `person@example.com` again, **Then** the system returns the existing redemption link rather than issuing a new one — the original invitation remains the source of truth.
3. **Given** a previous invitation for `person@example.com` has expired without being consumed, **When** the admin invites `person@example.com` again, **Then** the system issues a fresh invitation with a new redemption link and a new 7-day window.
4. **Given** a non-admin user (any future registered user without admin role), **When** they attempt to issue an invitation, **Then** the action is refused with a permission-denied response.
5. **Given** the admin submits a string that is not a valid email format, **When** the system validates the request, **Then** the request is rejected with a clear "invalid email" message and no invitation is recorded.

---

### User Story 3 - Invitee Accepts An Invitation And Signs In (Priority: P1)

The invitee receives the redemption link from the admin. They open it, complete
Google sign-in with the same email address the admin invited, and are accepted
into the system as a registered user. From this point forward they can sign in
on subsequent visits without needing the link again — their Google identity is
linked to their user record permanently.

**Why this priority**: This closes the onboarding loop. Without acceptance, an
invitation is a piece of paper with no signature. The admin's invitation is only
useful if the invitee can actually become a registered user.

**Independent Test**: Issue an invitation (US2), open the redemption link in a
browser, complete Google sign-in with the matching email, and confirm the
invitee lands on a signed-in landing page. Then sign the invitee out, sign
back in via the regular Google sign-in flow (no redemption link), and confirm
they are recognized as the same registered user.

**Acceptance Scenarios**:

1. **Given** an unconsumed and unexpired invitation exists for `person@example.com`, **When** they open the redemption link and complete Google sign-in with email `person@example.com`, **Then** the system creates a registered user record for them, marks the invitation as consumed, and presents the signed-in landing page.
2. **Given** an invitee has previously accepted an invitation and become a registered user, **When** they later sign in via the regular Google sign-in flow (no redemption link), **Then** the system recognizes them by their linked Google identity and signs them in without prompting for an invitation.
3. **Given** an invitee opens a redemption link whose token has expired, **When** they complete Google sign-in, **Then** the system refuses to register them with a clear message that the invitation has expired and instructs them to request a new one. No partial user record is created.
4. **Given** an invitee opens a redemption link whose token has already been consumed, **When** they attempt acceptance, **Then** the system refuses with a clear message that the invitation has already been used. The response does not reveal whether the token ever existed for a different user.
5. **Given** an invitee opens a redemption link addressed to `person@example.com` but completes Google sign-in with a DIFFERENT Google account email, **When** they attempt acceptance, **Then** the system refuses with a "this invitation was issued to a different email" message. No user record is created.
6. **Given** a person who has never been invited opens a fabricated or random redemption URL, **When** they complete Google sign-in, **Then** the system refuses acceptance with the same "invalid invitation" message used for expired/consumed tokens — the response gives no signal as to whether the token exists.

---

### Edge Cases

- **Admin email mismatch at seed time**: if the seeded admin email differs from the Google email the admin actually uses, the first sign-in attempt is rejected as "not registered." The admin must correct the seeded value and restart, or be invited like any other user (but no one can issue invites until the admin can sign in — bootstrap failure).
- **Concurrent acceptance attempts**: two requests with the same valid token arrive at the system simultaneously. Exactly one must succeed; the other must be rejected with the "already consumed" message. The system must not create two user records or leave the invitation in a half-consumed state.
- **Application restart mid-invitation**: an invitation issued before a restart must remain valid after the restart up to its expiration. Acceptance state must be durable.
- **Invitee changes their Google account email after acceptance**: the linked identity is the Google subject identifier (not the email), so a subsequent email change at Google does not break the user's ability to sign in.
- **Admin signs out and signs back in**: their session ends on sign-out; the next sign-in starts a fresh session. No re-linking is required.
- **System has no admin seeded**: if startup configuration is missing the admin email, the system must fail to start with a clear error rather than silently allow anyone to be the first admin.
- **Invitation issued to an email that already corresponds to a registered user**: this is a misuse case. The system must reject with a clear "already registered" message and not record the invitation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST be configurable with an administrator email at install time; if not configured, startup MUST fail with a clear error.
- **FR-002**: On first launch with admin email configured, system MUST create the administrator record so they can sign in.
- **FR-003**: System MUST require all users to authenticate via Google as the sole identity provider — no local passwords, no alternate identity sources.
- **FR-004**: On the administrator's first successful Google sign-in matching the configured email, system MUST permanently link the administrator's Google identity to their administrator record.
- **FR-005**: System MUST deny access to any Google account that has not been linked to a registered user record. The denial MUST distinguish "not registered" from "sign-in failed" and not reveal whether a similar email exists in the system.
- **FR-006**: Administrators MUST be able to issue invitations by submitting an invitee's email address. Non-administrators MUST be refused this action.
- **FR-007**: System MUST validate that submitted invitation emails are in a valid email format before recording an invitation.
- **FR-008**: System MUST refuse to issue an invitation for an email that already corresponds to a registered user record.
- **FR-009**: System MUST generate a single-use redemption token per invitation. Tokens MUST be cryptographically unguessable and MUST be stored in a form that cannot be replayed if the storage is compromised.
- **FR-010**: Invitations MUST expire 7 days after issuance and become non-redeemable on expiry.
- **FR-011**: When an invitation is requested for an email that already has an unconsumed, unexpired invitation, system MUST return the existing redemption link rather than create a duplicate.
- **FR-012**: When an invitation is requested for an email whose prior invitation has expired or been consumed, system MUST issue a fresh invitation.
- **FR-013**: Administrators MUST be presented with the redemption link upon successful invitation issuance so they can share it with the invitee out-of-band.
- **FR-014**: Invitees MUST be able to accept an invitation by opening the redemption link and completing Google sign-in with the email the invitation was issued to.
- **FR-015**: On successful acceptance, system MUST create a registered user record for the invitee, link their Google identity, mark the invitation as consumed, and sign them in.
- **FR-016**: System MUST refuse acceptance of expired tokens, consumed tokens, and tokens that do not correspond to any issued invitation. The refusal message MUST be identical in all three cases — the response MUST NOT reveal whether a token was once valid.
- **FR-017**: System MUST refuse acceptance when the invitee's Google account email does not exactly match the email the invitation was issued to.
- **FR-018**: Acceptance MUST be safe under concurrent requests: exactly one acceptance for a given token can succeed; all others MUST be refused with the standard refusal message.
- **FR-019**: Registered users (administrators and invitees who have accepted) MUST be able to sign in on subsequent visits via the regular Google sign-in flow without re-using a redemption link.
- **FR-020**: System MUST provide a sign-out action that ends the user's session.
- **FR-021**: System MUST record auth lifecycle events — sign-in, sign-out, invitation issued, invitation accepted, acceptance refused — for audit purposes. Records MUST NOT include the invitee's email address, the token value, or any personal name.
- **FR-022**: All authentication outcomes (signed in, not registered, invitation refused, etc.) MUST present user-facing messages that are clear without revealing whether the underlying record exists.

### Key Entities *(include if feature involves data)*

- **User**: A person who can sign in. Represents one human and their permanently-linked Google identity. Distinguishes administrators from standard users via a system role. Records when the user was first registered and the time of their last successful sign-in.
- **Invitation**: A pending offer for a person to become a registered user. Identifies the email the invitation was issued to, the administrator who issued it, the moment it was issued, the moment it expires, and (once accepted) the moment it was consumed and the user record it produced. The redemption token is stored in a non-replayable form.
- **AuthEvent**: An audit record of a notable auth-lifecycle moment (sign-in, sign-out, invitation issued, invitation accepted, acceptance refused). Identifies which user record was involved (when known) and the outcome, without recording sensitive identifiers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A first-time administrator can go from "configuration complete" to "signed in as admin" in under 60 seconds on a normal home internet connection.
- **SC-002**: 100% of sign-in attempts by Google accounts that have not been invited or seeded are refused. Zero such accounts gain access during any acceptance round of testing.
- **SC-003**: An administrator can issue an invitation and receive the redemption link in under 10 seconds from clicking the "invite" action.
- **SC-004**: An invitee, given a valid redemption link, can complete acceptance and reach the signed-in landing page in under 2 minutes from opening the link (assumes the invitee already has a Google account ready).
- **SC-005**: Expired tokens, consumed tokens, and never-issued tokens produce identical refusal messages — no information leakage. A user with a fabricated token cannot distinguish their case from a real invitee whose token expired.
- **SC-006**: Audit records for all auth lifecycle events are retrievable and contain zero personally-identifying values (email addresses, names, tokens). Verified by sampling logs after acceptance testing.
- **SC-007**: Across 20 simulated concurrent acceptance attempts on the same token, exactly one acceptance succeeds and the other 19 are refused with the standard refusal message. Zero duplicate user records are created.
- **SC-008**: Time from a registered user clicking "sign in" to landing on the signed-in landing page is under 5 seconds on subsequent visits (cached identity, no acceptance flow).

## Assumptions

- **Email delivery is out of scope for MVP**: invitation redemption links are returned directly to the administrator's screen for them to copy and share out-of-band. Sending invitation emails from the system (SMTP, transactional mail provider, etc.) is deferred until a future spec aligned with the constitution's MVP Implementation Order. This avoids any production email infrastructure for the local demo.
- **Invitations in this spec are user-level only**: the constitution's authorization model includes board-scoped invitations (Owner invites Member to a board). Because no boards exist yet, this spec scopes invitations to user-level only — "be a registered user of the system." Board-scoped invitations are layered on in a future spec when boards are introduced.
- **Strict email matching on acceptance**: the invitee's Google account email must exactly match the email the invitation was issued to. Personal-vs-work Google account flexibility is rejected for MVP — invitations are issued to a specific identity and must be redeemed by that identity. Considered more secure and matches user expectation; reversible later if a real product need surfaces.
- **One pending invitation per email at a time**: reinviting an email that already has an unconsumed, unexpired invitation returns the existing redemption link rather than revoking and reissuing. This keeps the redemption link stable until consumed or expired.
- **Single administrator seeded at install time**: this spec supports exactly one initial administrator. Mechanisms for adding additional administrators or promoting users to administrator are deferred to a later spec.
- **The seeded administrator's email is supplied via the existing local secrets workflow**: the operator configures the value on their machine before first launch.
- **Sessions are managed by the standard sign-in framework** without unusual lifetime requirements: sessions persist across browser refreshes for the duration of the standard session window and end on explicit sign-out.
- **Refusal messages favor security over diagnostic helpfulness**: when an invitation cannot be accepted, the user is told it is invalid without specifying why (expired vs. consumed vs. never existed). Administrators have access to audit records for support diagnosis.
- **No password reset / account recovery flow is required**: because Google is the sole identity provider, account recovery is delegated to Google. The system does not store recoverable credentials.
