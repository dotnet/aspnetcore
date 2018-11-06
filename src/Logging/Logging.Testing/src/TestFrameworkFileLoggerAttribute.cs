// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Logging.Testing
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public class TestFrameworkFileLoggerAttribute : Attribute
    {
        public TestFrameworkFileLoggerAttribute(string tfm, string baseDirectory = null)
        {
            TFM = tfm;
            BaseDirectory = baseDirectory;
        }

        public string TFM { get; }
        public string BaseDirectory { get; }
    }
}