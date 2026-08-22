using CorporateStarter.Application.Common.Security;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Infrastructure.Security
{
    public sealed class JwtSigningKeyOptionsValidator : IValidateOptions<JwtSigningKeyOptions>
    {
        private const int MinimumSecretBytes = 32;

        public ValidateOptionsResult Validate(
            string? name,
            JwtSigningKeyOptions options)
        {
            var failures = new List<string>();

            if (string.IsNullOrWhiteSpace(options.ActiveKeyId))
            {
                failures.Add("Active JWT signing key id is required.");
            }

            if (options.Keys.Count == 0)
            {
                failures.Add("At least one JWT signing key must be configured.");
                return ValidateOptionsResult.Fail(failures);
            }

            var duplicateKeyIds = options.Keys
                .Where(x => !string.IsNullOrWhiteSpace(x.KeyId))
                .GroupBy(x => x.KeyId, StringComparer.Ordinal)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            if (duplicateKeyIds.Length > 0)
            {
                failures.Add(
                    $"Duplicate JWT signing key ids are not allowed: {string.Join(", ", duplicateKeyIds)}.");
            }

            var now = DateTime.UtcNow;

            foreach (var key in options.Keys)
            {
                if (string.IsNullOrWhiteSpace(key.KeyId))
                {
                    failures.Add("JWT signing key id is required.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(key.Secret))
                {
                    failures.Add($"JWT signing key '{key.KeyId}' secret is required.");
                    continue;
                }

                if (Encoding.UTF8.GetByteCount(key.Secret) < MinimumSecretBytes)
                {
                    failures.Add(
                        $"JWT signing key '{key.KeyId}' secret must be at least {MinimumSecretBytes} bytes.");
                }

                if (key.NotBeforeUtc is not null
                    && key.NotAfterUtc is not null
                    && key.NotBeforeUtc >= key.NotAfterUtc)
                {
                    failures.Add(
                        $"JWT signing key '{key.KeyId}' NotBeforeUtc must be earlier than NotAfterUtc.");
                }
            }

            var activeKey = options.Keys.FirstOrDefault(x =>
                x.KeyId == options.ActiveKeyId);

            if (activeKey is null)
            {
                failures.Add("Active JWT signing key was not found in configured keys.");
            }
            else if (!activeKey.IsEnabled)
            {
                failures.Add("Active JWT signing key must be enabled.");
            }
            else if (activeKey.NotBeforeUtc is not null && activeKey.NotBeforeUtc > now)
            {
                failures.Add("Active JWT signing key is not valid yet.");
            }
            else if (activeKey.NotAfterUtc is not null && activeKey.NotAfterUtc <= now)
            {
                failures.Add("Active JWT signing key is expired.");
            }

            return failures.Count == 0
                ? ValidateOptionsResult.Success
                : ValidateOptionsResult.Fail(failures);
        }
    }
}