// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Constrains a dispatcher value parameter to represent only 32-bit integer values.
    /// </summary>
    public class IntDispatcherValueConstraint : IDispatcherValueConstraint
    {
        /// <inheritdoc />
        public bool Match(DispatcherValueConstraintContext constraintContext)
        {
            if (constraintContext == null)
            {
                throw new ArgumentNullException(nameof(constraintContext));
            }

            if (constraintContext.Values.TryGetValue(constraintContext.Key, out var value) && value != null)
            {
                if (value is int)
                {
                    return true;
                }

                var valueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                return int.TryParse(valueString, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result);
            }

            return false;
        }
    }
}