// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    public sealed class RazorLanguageServiceException : Exception
    {
        internal RazorLanguageServiceException(string callerClass, string callerMethod, Exception innerException)
            : base(Resources.FormatUnexpectedException(callerClass, callerMethod), innerException)
        {
        }
    }
}
