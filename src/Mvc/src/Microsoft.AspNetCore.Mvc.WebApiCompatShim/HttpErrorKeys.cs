// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Web.Http
{
    /// <summary>
    /// Provides keys to look up error information stored in the <see cref="HttpError"/> dictionary.
    /// </summary>
    public static class HttpErrorKeys
    {
        /// <summary>
        /// Provides a key for the Message.
        /// </summary>
        public static readonly string MessageKey = "Message";

        /// <summary>
        /// Provides a key for the MessageDetail.
        /// </summary>
        public static readonly string MessageDetailKey = "MessageDetail";

        /// <summary>
        /// Provides a key for the ModelState.
        /// </summary>
        public static readonly string ModelStateKey = "ModelState";

        /// <summary>
        /// Provides a key for the ExceptionMessage.
        /// </summary>
        public static readonly string ExceptionMessageKey = "ExceptionMessage";

        /// <summary>
        /// Provides a key for the ExceptionType.
        /// </summary>
        public static readonly string ExceptionTypeKey = "ExceptionType";

        /// <summary>
        /// Provides a key for the StackTrace.
        /// </summary>
        public static readonly string StackTraceKey = "StackTrace";

        /// <summary>
        /// Provides a key for the InnerException.
        /// </summary>
        public static readonly string InnerExceptionKey = "InnerException";

        /// <summary>
        /// Provides a key for the MessageLanguage.
        /// </summary>
        public static readonly string MessageLanguageKey = "MessageLanguage";

        /// <summary>
        /// Provides a key for the ErrorCode.
        /// </summary>
        public static readonly string ErrorCodeKey = "ErrorCode";
    }
}