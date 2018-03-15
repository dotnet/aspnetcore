// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using System;

using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.IdentityModel.Tokens;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class CryptographyIssuesTests : LoggedTest
    {
        public CryptographyIssuesTests(ITestOutputHelper output) : base(output)
        {
        }

        [ConditionalFact(Skip = "https://github.com/aspnet/Identity/issues/1630")]
        [FrameworkSkipCondition(RuntimeFrameworks.CoreCLR)]
        public void ImportRsaParameters_CLR()
        {
            RunTest(nameof(ImportRsaParameters_CLR));
        }

        [ConditionalFact(Skip = "https://github.com/aspnet/Identity/issues/1630")]
        [FrameworkSkipCondition(RuntimeFrameworks.CLR)]
        public void ImportRsaParameters_CoreCLR()
        {
            RunTest(nameof(ImportRsaParameters_CoreCLR));
        }

        private void RunTest(string testFlavor)
        {
            using (StartLog(out var loggerFactory, testFlavor))
            {
                var logger = loggerFactory.CreateLogger(testFlavor);
                for (var i = 0; i < 100; i++)
                {
                    var key = CryptoUtilities.CreateTestKey();
                    try
                    {
                        CryptographyHelpers.GetRSAParameters(new SigningCredentials(key, "RS256"));
                    }
                    catch (CryptographicException e)
                    {
                        LogKeyData(logger, i, key, e);
                        throw;
                    }
                }
            }
        }

        private static void LogKeyData(ILogger logger, int i, SecurityKey key, CryptographicException e)
        {
            logger.LogCritical(e.Message);
            logger.LogCritical($"Iteration {i}:");
            logger.LogCritical($"Key length: {key.KeySize}");
            var data = key as RsaSecurityKey;
            logger.LogCritical($"RSA Key length: {data.Rsa.KeySize}");
            RSAParameters parameters = data.Rsa.ExportParameters(includePrivateParameters: true);
            LogParameter(logger, "M", parameters.Modulus);
            LogParameter(logger, "E", parameters.Exponent);
            LogParameter(logger, "D", parameters.D);
            LogParameter(logger, "P", parameters.P);
            LogParameter(logger, "Q", parameters.Q);
            LogParameter(logger, "1/Q", parameters.InverseQ);
            LogParameter(logger, "DP", parameters.DP);
            LogParameter(logger, "DQ", parameters.DQ);
        }

        private static void LogParameter(ILogger logger, string name, byte[] parameter)
        {
            if (parameter == null)
            {
                logger.LogCritical($"Key parameter '{name}' is 'null'");
            }
            else
            {
                logger.LogCritical($"Key parameter '{name}' (Base64Encoded): '{Convert.ToBase64String(parameter)}'");
            }
        }
    }
}
