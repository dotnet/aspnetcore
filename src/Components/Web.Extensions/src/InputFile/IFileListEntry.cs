// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public interface IFileListEntry
    {
        string? Name { get; }

        DateTime? LastModified { get; }

        int Size { get; }

        string? Type { get; }

        string? RelativePath { get; }

        Stream Data { get; }

        event EventHandler OnDataRead;
    }
}
