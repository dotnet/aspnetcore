// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Constrains a dispatcher value parameter to contain only lowercase or uppercase letters A through Z in the English alphabet.
    /// </summary>
    public class AlphaDispatcherValueConstraint : RegexDispatcherValueConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AlphaDispatcherValueConstraint" /> class.
        /// </summary>
        public AlphaDispatcherValueConstraint() : base(@"^[a-z]*$")
        {
        }
    }
}