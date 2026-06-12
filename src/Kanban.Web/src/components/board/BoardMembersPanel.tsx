import { useState } from 'react'
import {
  Button,
  Field,
  Input,
  MessageBar,
  MessageBarBody,
  Select,
  Spinner,
  makeStyles,
  tokens,
} from '@fluentui/react-components'
import { useBoardMembers, type BoardMember } from '../../hooks/useBoardMembers'
import { useInviteBoardMember } from '../../hooks/useInviteBoardMember'
import { useChangeMemberRole } from '../../hooks/useChangeMemberRole'
import { useRemoveBoardMember } from '../../hooks/useRemoveBoardMember'

const useStyles = makeStyles({
  root: {
    padding: tokens.spacingHorizontalM,
    minWidth: '320px',
  },
  header: {
    display: 'flex',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: tokens.spacingVerticalM,
  },
  list: {
    listStyle: 'none',
    padding: 0,
    margin: 0,
  },
  item: {
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: tokens.spacingHorizontalS,
    padding: `${tokens.spacingVerticalS} 0`,
    borderBottom: `1px solid ${tokens.colorNeutralStroke2}`,
  },
  name: {
    flexGrow: 1,
  },
  inviteForm: {
    display: 'flex',
    flexDirection: 'column',
    gap: tokens.spacingVerticalS,
    marginBottom: tokens.spacingVerticalM,
  },
  inviteRow: {
    display: 'flex',
    gap: tokens.spacingHorizontalS,
  },
})

type BoardRole = 'Owner' | 'Member' | 'Viewer'

interface BoardMembersPanelProps {
  boardId: string
  callerRole: BoardRole
}

export default function BoardMembersPanel({ boardId, callerRole }: BoardMembersPanelProps) {
  const styles = useStyles()
  const [inviteOpen, setInviteOpen] = useState(false)
  const [inviteEmail, setInviteEmail] = useState('')
  const [inviteRole, setInviteRole] = useState<BoardRole>('Member')
  const [inviteSuccess, setInviteSuccess] = useState(false)

  const { data: members, isPending } = useBoardMembers(boardId)
  const inviteMutation = useInviteBoardMember()
  const changeRoleMutation = useChangeMemberRole()
  const removeMutation = useRemoveBoardMember()

  const isOwner = callerRole === 'Owner'
  const ownerCount = members?.filter((m) => m.role === 'Owner').length ?? 0

  function handleInvite() {
    if (!inviteEmail.trim()) return
    setInviteSuccess(false)
    inviteMutation.mutate(
      { boardId, email: inviteEmail.trim(), role: inviteRole },
      {
        onSuccess: () => {
          setInviteEmail('')
          setInviteOpen(false)
          setInviteSuccess(true)
        },
      }
    )
  }

  function handleRoleChange(member: BoardMember, newRole: BoardRole) {
    changeRoleMutation.mutate({ boardId, userId: member.userId, role: newRole })
  }

  function handleRemove(userId: string) {
    removeMutation.mutate({ boardId, userId })
  }

  return (
    <div className={styles.root}>
      <div className={styles.header}>
        <span>Members</span>
        {isOwner && (
          <Button size="small" onClick={() => setInviteOpen((o) => !o)} aria-label="Invite member">
            Invite
          </Button>
        )}
      </div>

      {inviteSuccess && (
        <MessageBar intent="success" role="alert">
          <MessageBarBody>Invite sent successfully.</MessageBarBody>
        </MessageBar>
      )}

      {inviteMutation.isError && (
        <MessageBar intent="error" role="alert">
          <MessageBarBody>
            {inviteMutation.error?.code === 'member.already_member'
              ? 'This user is already a board member.'
              : 'Failed to send invite. Please try again.'}
          </MessageBarBody>
        </MessageBar>
      )}

      {isOwner && inviteOpen && (
        <div className={styles.inviteForm}>
          <Field label="Email">
            <Input
              type="email"
              value={inviteEmail}
              onChange={(_, d) => setInviteEmail(d.value)}
              placeholder="name@example.com"
              aria-label="Email"
            />
          </Field>
          <Field label="Role">
            <Select
              value={inviteRole}
              onChange={(_, d) => setInviteRole(d.value as BoardRole)}
              aria-label="role"
            >
              <option value="Owner">Owner</option>
              <option value="Member">Member</option>
              <option value="Viewer">Viewer</option>
            </Select>
          </Field>
          <div className={styles.inviteRow}>
            <Button appearance="primary" onClick={handleInvite} disabled={inviteMutation.isPending}>
              Send invite
            </Button>
            <Button onClick={() => setInviteOpen(false)}>Cancel</Button>
          </div>
        </div>
      )}

      {isPending && <Spinner role="progressbar" label="Loading members…" />}

      {!isPending && members && (
        <ul className={styles.list} aria-label="Board members">
          {members.map((member) => {
            const isLastOwner = member.role === 'Owner' && ownerCount <= 1
            return (
              <li key={member.userId} className={styles.item}>
                <span className={styles.name}>{member.displayName}</span>

                {isOwner ? (
                  <Select
                    value={member.role}
                    onChange={(_, d) => handleRoleChange(member, d.value as BoardRole)}
                    aria-label="role"
                    disabled={isLastOwner}
                  >
                    <option value="Owner">Owner</option>
                    <option value="Member">Member</option>
                    <option value="Viewer">Viewer</option>
                  </Select>
                ) : (
                  <span>{member.role}</span>
                )}

                {isOwner && !isLastOwner && (
                  <Button
                    size="small"
                    appearance="subtle"
                    onClick={() => handleRemove(member.userId)}
                    disabled={removeMutation.isPending}
                    aria-label={`Remove ${member.displayName}`}
                  >
                    Remove
                  </Button>
                )}
              </li>
            )
          })}
        </ul>
      )}
    </div>
  )
}
