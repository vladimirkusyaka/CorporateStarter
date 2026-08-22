# CSRF Frontend Contract

CorporateStarter uses cookie-based refresh/logout endpoints. These endpoints require explicit CSRF protection.

The refresh token is stored in an HttpOnly cookie and is not readable by JavaScript. The CSRF token is stored in a separate non-HttpOnly cookie and must be sent back in a request header.

## Cookies

Refresh cookie:

- contains the refresh token;
- must be `HttpOnly`;
- must be `Secure` outside local development;
- should use `SameSite=Strict`;
- must not be read by frontend code.

CSRF cookie:

- contains the CSRF token;
- is readable by frontend code;
- must be copied into the configured CSRF request header;
- does not authenticate the user by itself.

## Required Header

For state-changing cookie endpoints, frontend code must send:

```http
X-CSRF-TOKEN: <csrf-cookie-value>


The configured header name is controlled by:
{
  "Csrf": {
    "HeaderName": "X-CSRF-TOKEN"
  }
}


Protected Endpoints
The following endpoints require CSRF header validation:
POST /api/Auth/refresh
POST /api/Auth/logout
POST /api/Auth/logout-all
Login Flow
POST /api/Auth/login does not require an existing CSRF token.
After successful login, the API returns:
access token in the JSON response;
refresh token in an HttpOnly cookie;
CSRF token in a readable cookie.
Frontend code must keep the access token only in memory.
Refresh Flow
When access token expires:
Read the CSRF cookie value.
Send POST /api/Auth/refresh with the CSRF header.
Browser sends the refresh cookie automatically.
API rotates the refresh token.
API returns a new access token.
API sets a new refresh cookie.
API sets a new CSRF cookie.
Forbidden Practices
Do not store access tokens or refresh tokens in browser local storage or session storage.
Do not send refresh tokens in JSON responses.
Do not make refresh/logout endpoints work without CSRF validation.