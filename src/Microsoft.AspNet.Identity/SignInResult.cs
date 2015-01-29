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
        public static SignInResult Success
        {
            get { return _success; }
        }

        /// <summary>
        ///     Static failure result
        /// </summary>
        /// <returns></returns>
        public static SignInResult Failed
        {
            get { return _failed; }
        }

        /// <summary>
        ///     Static locked out result
        /// </summary>
        /// <returns></returns>
        public static SignInResult LockedOut
        {
            get { return _lockedOut; }
        }

        /// <summary>
        ///     Static not allowed result
        /// </summary>
        /// <returns></returns>
        public static SignInResult NotAllowed
        {
            get { return _notAllowed; }
        }

        /// <summary>
        ///     Static two factor required result
        /// </summary>
        /// <returns></returns>
        public static SignInResult TwoFactorRequired
        {
            get { return _twoFactorRequired; }
        }

        /// <summary>
        ///     Log result based on properties
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="message"></param>
        public virtual void Log(ILogger logger, string message)
        {
            if (IsLockedOut)
            {
                logger.WriteInformation(Resources.FormatLoggingSigninResult(message, "Lockedout"));
            }
            else if (IsNotAllowed)
            {
                logger.WriteInformation(Resources.FormatLoggingSigninResult(message, "NotAllowed"));
            }
            else if (RequiresTwoFactor)
            {
                logger.WriteInformation(Resources.FormatLoggingSigninResult(message, "RequiresTwoFactor"));
            }
            else if (Succeeded)
            {
                logger.WriteInformation(Resources.FormatLoggingSigninResult(message, "Succeeded"));
            }
            else
            {
                logger.WriteInformation(Resources.FormatLoggingSigninResult(message, "Failed"));
            }
        }
    }
}