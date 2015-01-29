// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class SignInResultTest
    {
        [Fact]
        public void VerifyLogSuccess()
        {
            var result = SignInResult.Success;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Result : Succeeded", logMessage.ToString());
        }

        [Fact]
        public void VerifyLogLockedOut()
        {
            var result = SignInResult.LockedOut;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Result : Lockedout", logMessage.ToString());
        }

        [Fact]
        public void VerifyLogNotAllowed()
        {
            var result = SignInResult.NotAllowed;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Result : NotAllowed", logMessage.ToString());
        }

        [Fact]
        public void VerifyLogRequiresTwoFactor()
        {
            var result = SignInResult.TwoFactorRequired;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Result : RequiresTwoFactor", logMessage.ToString());
        }

        [Fact]
        public void VerifyLogRequiresFailed()
        {
            var result = SignInResult.Failed;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Result : Failed", logMessage.ToString());
        }
    }
}