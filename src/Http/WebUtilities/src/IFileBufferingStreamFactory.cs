// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace Microsoft.AspNetCore.WebUtilities
{
    public interface IFileBufferingStreamFactory
    {
        Stream CreateReadStream(Stream inner, int bufferThreshold, long? bufferLimit = null);
        IBufferedWriteStream CreateWriteStream();
        IBufferedWriteStream CreateWriteStream(int memoryThreshold, long? bufferLimit = null);
    }
}
