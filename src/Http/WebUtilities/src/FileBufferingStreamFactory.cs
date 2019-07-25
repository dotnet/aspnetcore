// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.WebUtilities
{
    public class FileBufferingStreamFactory : IFileBufferingStreamFactory
    {
        private readonly string _tempDirectoryValue;
        private readonly Func<string> _getTempDirectory;

        public FileBufferingStreamFactory(string tempDirectoryPath)
        {
            _tempDirectoryValue = tempDirectoryPath;
            _getTempDirectory = () => _tempDirectory;
        }

        public Stream CreateReadStream(Stream inner, int bufferThreshold, long? bufferLimit = null)
        {
            return new FileBufferingReadStream(inner, bufferThreshold, bufferLimit, _getTempDirectory);
        }

        public IBufferedWriteStream CreateWriteStream()
        {
            return new FileBufferingWriteStream(tempFileDirectoryAccessor: _getTempDirectory);
        }

        public IBufferedWriteStream CreateWriteStream(int memoryThreshold, long? bufferLimit = null)
        {
            return new FileBufferingWriteStream(memoryThreshold, bufferLimit, _getTempDirectory);
        }

        private string _tempDirectory
        {
            get
            {
                if (!Directory.Exists(_tempDirectoryValue))
                {
                    throw new DirectoryNotFoundException(_tempDirectoryValue);
                }

                return _tempDirectoryValue;
            }
        }
    }
}

