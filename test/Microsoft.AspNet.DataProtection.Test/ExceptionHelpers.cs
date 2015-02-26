// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNet.DataProtection.Test
{
    internal static class ExceptionHelpers
    {
        public static void AssertMessage(this ArgumentException exception, string parameterName, string message)
        {
            Assert.Equal(parameterName, exception.ParamName);

            // We'll let ArgumentException handle the message formatting for us and treat it as our control value
            var controlException = new ArgumentException(message, parameterName);
            Assert.Equal(controlException.Message, exception.Message);
        }
    }
}
