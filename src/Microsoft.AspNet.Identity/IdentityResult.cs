// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Represents the result of an identity operation
    /// </summary>
    public class IdentityResult
    {
        private static readonly IdentityResult _success = new IdentityResult { Succeeded = true };

        private List<IdentityError> _errors = new List<IdentityError>();

        /// <summary>
        ///     True if the operation was successful
        /// </summary>
        public bool Succeeded { get; protected set; }

        /// <summary>
        ///     List of errors
        /// </summary>
        public IEnumerable<IdentityError> Errors { get { return _errors; } }

        /// <summary>
        ///     Static success result
        /// </summary>
        /// <returns></returns>
        public static IdentityResult Success
        {
            get { return _success; }
        }

        /// <summary>
        ///     Failed helper method
        /// </summary>
        /// <param name="errors"></param>
        /// <returns></returns>
        public static IdentityResult Failed(params IdentityError[] errors)
        {
            var result = new IdentityResult { Succeeded = false };
            if (errors != null)
            {
                result._errors.AddRange(errors);
            }
            return result;
        }

        /// <summary>
        ///     Log Identity result
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public virtual void Log(ILogger logger, string message)
        {
            // TODO: Take logging level as a parameter
            if (Succeeded)
            {
                logger.WriteInformation(Resources.FormatLogIdentityResultSuccess(message));
            }
            else
            {
                logger.WriteWarning(Resources.FormatLogIdentityResultFailure(message, string.Join(",", Errors.Select(x => x.Code).ToList())));
            }
        }
    }
}