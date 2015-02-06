// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Identity
{
    /// <summary>
    ///     Represents the result of an sign in operation
    /// </summary>
    public class SignInResult
    {
        private static readonly SignInResult _success = new SignInResult { Succeeded = true };
        private static readonly SignInResult _failed = new SignInResult();
        private static readonly SignInResult _lockedOut = new SignInResult { IsLockedOut = true };
        private static readonly SignInResult _notAllowed = new SignInResult { IsNotAllowed = true };
        private static readonly SignInResult _twoFactorRequired = new SignInResult { RequiresTwoFactor = true };

        /// <summary>
        ///     True if the operation was successful
        /// </summary>
        public bool Succeeded { get; protected set; }

        /// <summary>
        ///     True if the user is locked out
        /// </summary>
        public bool IsLockedOut { get; protected set; }

        /// <summary>
        ///     True if the user is not allowed to sign in
        /// </summary>
        public bool IsNotAllowed { get; protected set; }

        /// <summary>
        ///     True if the sign in requires two factor
        /// </summary>
        public bool RequiresTwoFactor { get; protected set; }

        /// <summary>
        ///     Static success result
        /// </summary>
        /// <returns></returns>
        public static SignInResult Success => _success;

        /// <summary>
        ///     Static failure result
        /// </summary>
        /// <returns></returns>
        public static SignInResult Failed => _failed;

        /// <summary>
        ///     Static locked out result
        /// </summary>
        /// <returns></returns>
        public static SignInResult LockedOut => _lockedOut;

        /// <summary>
        ///     Static not allowed result
        /// </summary>
        /// <returns></returns>
        public static SignInResult NotAllowed => _notAllowed;

        /// <summary>
        ///     Static two factor required result
        /// </summary>
        /// <returns></returns>
        public static SignInResult TwoFactorRequired => _twoFactorRequired;

        /// <summary>
        ///     Returns string representation of the result. 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return IsLockedOut ? "Lockedout" : 
		   	       IsNotAllowed ? "NotAllowed" : 
			       RequiresTwoFactor ? "RequiresTwoFactor" : 
			       Succeeded ? "Succeeded" : "Failed";
        }

        /// <summary>
        ///     Returns the level at which this result should be logged
        /// </summary>
        /// <returns></returns>
        public virtual LogLevel GetLogLevel()
        {
            return Succeeded || RequiresTwoFactor ? LogLevel.Verbose : LogLevel.Warning;
        }
    }
}