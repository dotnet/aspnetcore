// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Represents the collection of files sent with the HttpRequest.
    /// </summary>
    public interface IFormFileCollection : IReadOnlyList<IFormFile>
    {
        IFormFile this[string name] { get; }

        IFormFile GetFile(string name);

        IReadOnlyList<IFormFile> GetFiles(string name);
    }
}