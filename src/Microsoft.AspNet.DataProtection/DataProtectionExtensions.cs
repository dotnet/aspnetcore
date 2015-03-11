// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.DataProtection
{
    /// <summary>
    /// Helpful extension methods for data protection APIs.
    /// </summary>
    public static class DataProtectionExtensions
    {
        /// <summary>
        /// Creates a time-limited data protector based on an existing protector.
        /// </summary>
        /// <param name="protector">The existing protector from which to derive a time-limited protector.</param>
        /// <returns>A time-limited data protector.</returns>
        public static ITimeLimitedDataProtector AsTimeLimitedDataProtector([NotNull] this IDataProtector protector)
        {
            return (protector as ITimeLimitedDataProtector)
                ?? new TimeLimitedDataProtector(protector.CreateProtector(TimeLimitedDataProtector.PurposeString));
        }
    }
}
