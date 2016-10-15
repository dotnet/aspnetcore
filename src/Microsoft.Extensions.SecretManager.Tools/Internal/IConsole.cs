// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.Extensions.SecretManager.Tools.Internal
{
    public interface IConsole
    {
        TextWriter Out { get; }
        TextWriter Error { get; }
        TextReader In { get; }
        bool IsInputRedirected { get; }
    }
}
