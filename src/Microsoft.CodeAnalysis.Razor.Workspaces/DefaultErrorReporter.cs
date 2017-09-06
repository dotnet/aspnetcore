// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Razor
{
    internal class DefaultErrorReporter : ErrorReporter
    {
        public override void ReportError(Exception exception)
        {
            // Do nothing.
        }

        public override void ReportError(Exception exception, Project project)
        {
            // Do nothing.
        }
    }
}
