// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Dispatcher
{
    /// <summary>
    /// Represents a regex constraint.
    /// </summary>
    public class RegexStringDispatcherValueConstraint : RegexDispatcherValueConstraint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegexStringDispatcherValueConstraint" /> class.
        /// </summary>
        /// <param name="regexPattern">The regular expression pattern to match.</param>
        public RegexStringDispatcherValueConstraint(string regexPattern)
            : base(regexPattern)
        {
        }
    }
}
