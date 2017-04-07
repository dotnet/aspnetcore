// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    public static class ExceptionHelpers
    {
        public static void ValidateArgumentException(string parameterName, string expectedMessage, ArgumentException exception)
        {
            Assert.Equal(string.Format("{0}{1}Parameter name: {2}", expectedMessage, Environment.NewLine, parameterName), exception.Message);
        }
    }
}