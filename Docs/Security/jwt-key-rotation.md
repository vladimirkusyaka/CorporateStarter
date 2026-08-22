# JWT Signing Key Rotation

CorporateStarter uses a JWT signing key ring.

Only the key specified by `JwtSigningKeys:ActiveKeyId` is used to sign new access tokens. Enabled non-expired keys remain valid for token validation while issued access tokens are still alive.

## Configuration

```json
{
  "JwtSigningKeys": {
    "ActiveKeyId": "key-2026-08",
    "Keys": [
      {
        "KeyId": "key-2026-08",
        "Secret": "<secret-at-least-32-bytes>",
        "IsEnabled": true,
        "NotBeforeUtc": "2026-08-01T00:00:00Z",
        "NotAfterUtc": null
      }
    ]
  }
}


Rotation Procedure
Add a new enabled key to JwtSigningKeys:Keys.
Keep the previous key enabled.
Set JwtSigningKeys:ActiveKeyId to the new key id.
Deploy the configuration.
Wait until all access tokens signed with the previous key have expired.
Disable or remove the previous key.
Deploy the cleanup configuration.


Emergency Rotation
If a signing key is suspected to be compromised:
Add a new signing key.
Set it as active immediately.
Disable the compromised key.
Deploy immediately.
Revoke active sessions if compromise impact includes refresh tokens or session state.
Review SecurityEvents for suspicious activity.
Emergency rotation invalidates access tokens signed by the disabled key.


