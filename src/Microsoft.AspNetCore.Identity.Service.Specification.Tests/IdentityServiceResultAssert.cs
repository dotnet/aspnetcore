// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Identity.Service;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Test
{
    /// <summary>
    /// Helper for tests to validate identity results.
    /// </summary>
    public static class IdentityServiceResultAssert
    {
        /// <summary>
        /// Asserts that the result has Succeeded.
        /// </summary>
        /// <param name="result"></param>
        public static void IsSuccess(IdentityServiceResult result)
        {
            Assert.NotNull(result);
            Assert.True(result.Succeeded);
        }

        /// <summary>
        /// Asserts that the result has not Succeeded.
        /// </summary>
        public static void IsFailure(IdentityServiceResult result)
        {
            Assert.NotNull(result);
            Assert.False(result.Succeeded);
        }

        /// <summary>
        /// Asserts that the result has not Succeeded and that error is the first Error's Description.
        /// </summary>
        public static void IsFailure(IdentityServiceResult result, string error)
        {
            Assert.NotNull(result);
            Assert.False(result.Succeeded);
            Assert.Equal(error, result.Errors.First().Description);
        }

        /// <summary>
        /// Asserts that the result has not Succeeded and that first error matches error's code and Description.
        /// </summary>
        public static void IsFailure(IdentityServiceResult result, IdentityServiceError error)
        {
            Assert.NotNull(result);
            Assert.False(result.Succeeded);
            Assert.Equal(error.Description, result.Errors.First().Description);
            Assert.Equal(error.Code, result.Errors.First().Code);
        }
    }
}