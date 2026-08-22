# Security Events

CorporateStarter writes security events for authentication and session-sensitive operations.

Security events are separate from general audit logs. Audit logs describe data changes. Security events describe authentication, authorization, session, and suspicious activity.

## Event Fields

A security event should include:

- event type;
- severity;
- outcome;
- created time;
- correlation id;
- subject user id;
- subject user email;
- auth session id;
- refresh token family id;
- IP address;
- user agent;
- additional details.

## Event Types

Recommended event types:

- `LoginSucceeded`
- `LoginFailed`
- `LoginLockedOut`
- `LogoutSucceeded`
- `LogoutAllSucceeded`
- `RefreshSucceeded`
- `RefreshFailed`
- `RefreshTokenReuseDetected`
- `SessionRevoked`
- `MfaChallengeCreated`
- `MfaEnrollmentRequired`
- `PasswordChanged`
- `PasswordPolicyRejected`

## Severity

Recommended severity levels:

- `Low` for normal successful authentication activity;
- `Medium` for failed login, lockout, MFA challenge, or password policy rejection;
- `High` for refresh token reuse or suspicious session activity;
- `Critical` for confirmed compromise or administrative emergency response.

## Retention

Security events should be retained according to company policy and legal requirements.

For corporate applications, security events are commonly retained longer than ordinary operational logs.

## Privacy

Security events may contain personal data such as IP address, email, user agent, and user id.

Do not log passwords, refresh tokens, access tokens, CSRF tokens, MFA secrets, or raw credentials.