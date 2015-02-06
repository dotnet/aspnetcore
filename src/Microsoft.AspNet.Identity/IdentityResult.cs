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
        public IEnumerable<IdentityError> Errors => _errors;

        /// <summary>
        ///     Static success result
        /// </summary>
        /// <returns></returns>
        public static IdentityResult Success => _success;

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
        ///     Return string representation of IdentityResult
        /// </summary>
        /// <returns>"Succedded", if result is suceeded else "Failed:error codes"</returns>
        public override string ToString()
        {
            return Succeeded ? 
                   "Succeeded" : 
                   string.Format("{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
        }

        /// <summary>
        ///     Get the level to log this result
        /// </summary>
        /// <returns>LogLevel to log</returns>
        public virtual LogLevel GetLogLevel()
        {
            return Succeeded ? LogLevel.Verbose : LogLevel.Warning;
        }
    }
}