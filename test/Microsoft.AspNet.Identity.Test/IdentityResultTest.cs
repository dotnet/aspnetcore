// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Text;
using Xunit;

namespace Microsoft.AspNet.Identity.Test
{
    public class IdentityResultTest
    {
        [Fact]
        public void VerifyDefaultConstructor()
        {
            var result = new IdentityResult();
            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }

        [Fact]
        public void NullFailedUsesEmptyErrors()
        {
            var result = IdentityResult.Failed();
            Assert.False(result.Succeeded);
            Assert.Equal(0, result.Errors.Count());
        }

        [Fact]
        public void VerifySuccessResultLog()
        {
            var result = IdentityResult.Success;
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Success", logMessage.ToString());
        }

        [Fact]
        public void VerifyFailureResultLog()
        {
            var result = IdentityResult.Failed(new IdentityError() { Code = "Foo" }, new IdentityError() { Code = "Bar" });
            var logMessage = new StringBuilder();
            var logger = MockHelpers.MockILogger(logMessage);

            result.Log(logger.Object, "Operation");

            Assert.Equal("Operation : Failed : Foo,Bar", logMessage.ToString());
        }
    }
}