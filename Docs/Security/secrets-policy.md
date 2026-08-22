# Secrets Policy

CorporateStarter must not store production secrets in source control.

This includes:

- JWT signing keys;
- database passwords;
- SMTP/API credentials;
- external provider client secrets;
- production admin passwords;
- encryption keys;
- private certificates.

## Configuration Sources

Production secrets should be provided through an approved secret source:

- environment variables;
- container orchestration secrets;
- cloud secret manager;
- enterprise vault;
- CI/CD protected variables.

Development secrets may use local user secrets or local environment variables.

## JWT Signing Keys

JWT signing keys must be at least 32 bytes.

Production JWT signing keys must be generated with a cryptographically secure random generator.

Example PowerShell command:

```powershell
$bytes = New-Object byte[] 64
[System.Security.Cryptography.RandomNumberGenerator]::Fill($bytes)
[Convert]::ToBase64String($bytes)


Repository Rules
Never commit:
real production connection strings;
real signing keys;
real passwords;
.env files with secrets;
exported production configuration;
database backups containing customer data.
Example configuration files may contain placeholder values only.
Incident Response
If a secret is committed:
Treat it as compromised.
Remove it from current source.
Rotate the secret in the real environment.
Revoke dependent sessions/tokens if applicable.
Review logs for unauthorized use.
Clean repository history only according to the team's approved process.