// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Editor
{
    internal class ForegroundThreadAffinitizedObject
    {
        private readonly Thread _foregroundThread;

        public ForegroundThreadAffinitizedObject()
        {
            _foregroundThread = Thread.CurrentThread;
        }

        public void AssertIsForeground()
        {
            if (Thread.CurrentThread != _foregroundThread)
            {
                throw new InvalidOperationException("Expected to be on the foreground thread and was not.");
            }
        }

        public void AssertIsBackground()
        {
            if (Thread.CurrentThread == _foregroundThread)
            {
                throw new InvalidOperationException("Expected to be on a background thread and was not.");
            }
        }
    }
}
