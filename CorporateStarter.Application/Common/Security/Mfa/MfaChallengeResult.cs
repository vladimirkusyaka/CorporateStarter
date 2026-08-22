using System;
using System.Collections.Generic;
using System.Text;

namespace CorporateStarter.Application.Common.Security.Mfa
{
    public sealed class MfaChallengeResult
    {
        public bool Succeeded { get; set; }

        public Guid? ChallengeId { get; set; }

        public MfaProviderType? ProviderType { get; set; }

        public string? PublicChallengeDataJson { get; set; }

        public string? ErrorCode { get; set; }

        public string? ErrorMessage { get; set; }

        public static MfaChallengeResult NotRequired()
        {
            return new MfaChallengeResult
            {
                Succeeded = true
            };
        }

        public static MfaChallengeResult ChallengeCreated(
            Guid challengeId,
            MfaProviderType providerType,
            string? publicChallengeDataJson)
        {
            return new MfaChallengeResult
            {
                Succeeded = false,
                ChallengeId = challengeId,
                ProviderType = providerType,
                PublicChallengeDataJson = publicChallengeDataJson
            };
        }

        public static MfaChallengeResult Failure(
            string errorCode,
            string errorMessage)
        {
            return new MfaChallengeResult
            {
                Succeeded = false,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
