// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Http
{
    public class HttpFileBufferingStreamFactory : IFileBufferingStreamFactory
    {
        private readonly HttpBufferingOptions _bufferingOptions;
        private readonly Func<string> _getTempDirectory;

        public HttpFileBufferingStreamFactory(IOptions<HttpBufferingOptions> bufferingOptions)
        {
            if (bufferingOptions == null)
            {
                throw new ArgumentNullException(nameof(bufferingOptions));
            }

            _bufferingOptions = bufferingOptions.Value;
            _getTempDirectory = () => _tempDirectory;
        }

        public FileBufferingReadStream CreateReadStream(Stream inner, int bufferThreshold, long? bufferLimit = null)
        {
            return new FileBufferingReadStream(inner, bufferThreshold, bufferLimit, _getTempDirectory);
        }

        public FileBufferingWriteStream CreateWriteStream()
        {
            return new FileBufferingWriteStream(tempFileDirectoryAccessor: _getTempDirectory);
        }

        public FileBufferingWriteStream CreateWriteStream(int memoryThreshold, long? bufferLimit = null)
        {
            return new FileBufferingWriteStream(memoryThreshold, bufferLimit, _getTempDirectory);
        }

        private string _tempDirectory
        {
            get
            {
                // TempFileDirectory defaults to Path.GetTempPath() if it's not modified
                if (!Directory.Exists(_bufferingOptions.TempFileDirectory))
                {
                    throw new DirectoryNotFoundException(_bufferingOptions.TempFileDirectory);
                }

                return _bufferingOptions.TempFileDirectory;
            }
        }
    }
}
