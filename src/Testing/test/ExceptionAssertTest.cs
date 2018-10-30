// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class ExceptionAssertTest
    {
        [Fact]
        [ReplaceCulture("fr-FR", "fr-FR")]
        public void AssertArgumentNullOrEmptyString_WorksInNonEnglishCultures()
        {
            // Arrange
            Action action = () =>
            {
                throw new ArgumentException("Value cannot be null or an empty string.", "foo");
            };

            // Act and Assert
            ExceptionAssert.ThrowsArgumentNullOrEmptyString(action, "foo");
        }

        [Fact]
        [ReplaceCulture("fr-FR", "fr-FR")]
        public void AssertArgumentOutOfRangeException_WorksInNonEnglishCultures()
        {
            // Arrange
            Action action = () =>
            {
                throw new ArgumentOutOfRangeException("foo", 10, "exception message.");
            };

            // Act and Assert
            ExceptionAssert.ThrowsArgumentOutOfRange(action, "foo", "exception message.", 10);
        }
    }
}