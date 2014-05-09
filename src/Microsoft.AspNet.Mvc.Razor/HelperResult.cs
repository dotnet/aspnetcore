// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class HelperResult
    {
        private readonly Action<TextWriter> _action;

        public HelperResult([NotNull] Action<TextWriter> action)
        {
            _action = action;
        }

        public void WriteTo([NotNull] TextWriter writer)
        {
            _action(writer);
        }
    }
}
