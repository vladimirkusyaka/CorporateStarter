# Refresh Token Incident Response

CorporateStarter stores only refresh token hashes in the database. Refresh tokens are transported only through HttpOnly cookies.

Refresh token reuse is treated as a security incident.

## Reuse Detection

A refresh token is considered reused when a revoked or replaced refresh token is presented again.

Expected response:

- reject the refresh request;
- revoke the related token family;
- revoke the related auth session;
- write a security event;
- require the user to authenticate again.

## Investigation Checklist

1. Find the related `SecurityEvents` record.
2. Identify `SubjectUserId`, `AuthSessionId`, and `RefreshTokenFamilyId`.
3. Review IP address and user agent.
4. Review active sessions for the same user.
5. Revoke suspicious sessions.
6. Ask the user to change password if credential compromise is suspected.
7. Consider rotating JWT signing keys only if signing keys may be exposed.

## Containment Actions

Use session governance endpoints:

- `GET /api/Auth/sessions`
- `DELETE /api/Auth/sessions/{authSessionId}`
- `POST /api/Auth/logout-all`

`logout-all` should be used when the account itself is suspected to be compromised.