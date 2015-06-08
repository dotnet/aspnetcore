// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    /// Options for user validation.
    /// </summary>
    public class UserOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserOptions"/> class.
        /// </summary>
        public UserOptions()
        {
            //User.RequireUniqueEmail = true; // TODO: app decision?
        }

        /// <summary>
        /// Gets or sets the regular expression used to validate user names.
        /// </summary>
        /// <value>
        /// The regular expression used to validate user names.
        /// </value>
        /// <remarks>
        /// As regular expressions can be subject to Denial of Service attacks, depending on their complexity and user input,
        /// validation via regular expressions will timeout and fail after the value set in the <see cref="UserNameValidationRegexTimeout"/>
        /// property.
        /// </remarks>
        public string UserNameValidationRegex { get; set; } = "^[a-zA-Z0-9@_\\.]+$";

        /// <summary>
        /// Gets or sets the timeout value used after which user name validation via the <see cref="UserNameValidationRegex"/> will fail if it has
        /// not completed.
        /// </summary>
        /// <value>
        /// The timeout value used after which user name validation via the <see cref="UserNameValidationRegex"/> will fail if it has not completed.
        /// </value>
        /// <remarks>
        /// The default value is 20 milliseconds.
        /// </remarks>
        public TimeSpan UserNameValidationRegexTimeout { get; set; } = new TimeSpan(0,0,0,0,20);

        /// <summary>
        /// Gets or sets a flag indicating whether the application requires unique emails for its users.
        /// </summary>
        /// <value>
        /// True if the application requires each user to have their own, unique email, otherwise false.
        /// </value>
        public bool RequireUniqueEmail { get; set; }
    }
}