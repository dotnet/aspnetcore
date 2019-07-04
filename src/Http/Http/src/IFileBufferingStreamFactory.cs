// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Http
{
    public interface IFileBufferingStreamFactory
    {
        Stream CreateReadStream(Stream inner, int bufferThreshold, long? bufferLimit = null);
        FileBufferingWriteStream CreateWriteStream();
        FileBufferingWriteStream CreateWriteStream(int memoryThreshold, long? bufferLimit = null);
    }
}
