# Auth Security Checklist

This checklist describes the CorporateStarter local-auth security baseline.

It does not cover external identity provider integration, Active Directory, SAML, OpenID Connect federation, or customer-specific compliance requirements.

## Token Handling

- Access tokens are short-lived.
- Access tokens are returned in API responses and must be kept only in memory by frontend code.
- Refresh tokens are transported only in HttpOnly cookies.
- Refresh tokens are not stored in browser local storage or session storage.
- Refresh tokens are stored in the database only as hashes.
- Refresh tokens are rotated on refresh.
- Reused refresh tokens are rejected and treated as suspicious activity.

## Cookie Security

- Refresh cookie is HttpOnly.
- Refresh cookie is Secure outside local development.
- Refresh cookie uses SameSite policy.
- CSRF token is stored separately from the refresh token.
- Cookie-based state-changing auth endpoints require explicit CSRF validation.

## Session Governance

- Successful login creates a server-side auth session.
- Access tokens are bound to auth session id.
- Users can list active sessions.
- Users can revoke a specific active session.
- Users can revoke all active sessions.
- Logout revokes the current session.
- Refresh token reuse revokes related session state.

## Login Protection

- Login is rate-limited.
- Refresh is rate-limited.
- Failed login attempts are tracked.
- Repeated failed login attempts trigger lockout or progressive delay.
- Login failures should not reveal whether login or password was incorrect.

## Password Governance

- Password policy is enforced.
- Weak passwords are rejected.
- Recent password reuse is rejected.
- Password hashes are created with a password hashing algorithm suitable for password storage.
- Raw passwords are never written to logs or audit records.

## JWT Signing Keys

- JWT signing keys are configured as a key ring.
- Only the active key signs new tokens.
- Previous enabled keys can validate tokens during rotation.
- Signing key configuration is validated.
- Signing keys must not be committed to source control.

## Security Events

- Authentication-sensitive actions write security events.
- Security events are separate from general audit logs.
- Security events include event type, severity, outcome, subject user, IP address, user agent, and correlation data where available.
- Tokens, passwords, MFA secrets, and raw credentials must never be logged.

## MFA Extension Points

- MFA policy extension point exists.
- MFA challenge creation extension point exists.
- Default implementation does not enforce MFA.
- Customer-specific MFA providers can be integrated without changing the login contract.

## Configuration Validation

- JWT signing key configuration is validated.
- Refresh token configuration is validated.
- CSRF configuration is validated.
- Invalid security configuration is covered by tests.

## Required Before Production Deployment

- Replace all sample secrets.
- Configure production JWT signing keys through approved secret storage.
- Configure production database credentials through approved secret storage.
- Enforce HTTPS.
- Keep refresh cookies Secure.
- Configure CORS allowlist for real frontend origins.
- Review security headers.
- Review log retention and privacy requirements.
- Review password policy with the customer.
- Decide whether MFA is mandatory.
- Decide session lifetime and idle-timeout policy.
- Run integration tests.