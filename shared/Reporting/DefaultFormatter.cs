// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Tools.Internal
{
    public class DefaultFormatter : IFormatter
    {
        public static readonly IFormatter Instance = new DefaultFormatter();

        private DefaultFormatter() {}
        public string Format(string text)
            => text;
    }
}