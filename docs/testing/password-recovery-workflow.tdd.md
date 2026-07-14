# Password recovery workflow — TDD evidence

## RED

`PasswordRecoveryFlowTests` was added before the implementation. The first run failed to compile because `ApplicationDbContext` did not expose `PasswordResetRequests` (`CS1061`).

## GREEN

The implementation now covers:

- a generic operator request that does not reveal whether an account exists;
- an administrator-only approval action;
- immediate invalidation of the old password and existing sessions on approval;
- one-time passwordless recovery access, expiring after 15 minutes;
- middleware that restricts the recovery session to password change and logout;
- mandatory password replacement and security-stamp rotation;
- prevention of recovery replay after completion.

Targeted verification: 3 passed, 0 failed.
