# Session Governance

CorporateStarter uses server-side auth sessions.

Each successful login creates:

- an auth session;
- a refresh token family;
- an initial refresh token;
- an access token bound to the auth session id.

Access tokens contain the `auth_session_id` claim. Refresh tokens are linked to the same server-side auth session.

## User Session Endpoints

Authenticated users can inspect and manage their own sessions.

```http
GET /api/Auth/sessions

Returns active sessions for the current user.

DELETE /api/Auth/sessions/{authSessionId}

Revokes one active session owned by the current user.

POST /api/Auth/logout

Revokes the current session.

POST /api/Auth/logout-all

Revokes all active sessions for the current user.
Current Session
The current session is resolved from the authenticated access token by reading the auth_session_id claim.
If the access token does not contain a valid session id, session governance endpoints should reject the request.
Revocation Rules
Revoking a session should revoke:
the auth session;
active refresh tokens linked to that session;
refresh token families linked to that session.
Existing access tokens may remain cryptographically valid until expiration. CorporateStarter uses short-lived access tokens to limit this window.
Refresh Token Reuse
Refresh token reuse is treated as a possible token theft signal.
When reuse is detected, the system should revoke the related token family and auth session, then write a security event.
Depending on policy, the implementation may also revoke all user sessions.
Frontend Behavior
Frontend code should call GET /api/Auth/sessions to show active sessions.
When a user revokes a session:
if it is not the current session, remove it from the list;
if it is the current session, clear in-memory auth state and return to the locked/login state.
Frontend code must not store refresh tokens or access tokens in browser storage.























