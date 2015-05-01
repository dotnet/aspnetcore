// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNet.Http
{
    public interface IFormFile
    {
        string ContentType { get; }

        string ContentDisposition { get; }

        IHeaderDictionary Headers { get; }

        long Length { get; }

        Stream OpenReadStream();
    }
}