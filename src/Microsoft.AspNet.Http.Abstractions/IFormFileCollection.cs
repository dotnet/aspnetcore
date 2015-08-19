// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Http
{
    public interface IFormFileCollection : IReadOnlyList<IFormFile>
    {
        IFormFile this[string name] { get; }

        IFormFile GetFile(string name);

        IReadOnlyList<IFormFile> GetFiles(string name);
    }
}