// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using Microsoft.AspNet.FileSystems;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class TestFileInfo : IFileInfo
    {
        private string _content;

        public bool IsDirectory { get; } = false;

        public DateTime LastModified { get; set; }

        public long Length { get; set; }

        public string Name { get; set; }

        public string PhysicalPath { get; set; }

        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                Length = Encoding.UTF8.GetByteCount(Content);
            }
        }

        public bool Exists
        {
            get { return true; }
        }

        public bool IsReadOnly
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public Stream CreateReadStream()
        {
            var bytes = Encoding.UTF8.GetBytes(Content);
            return new MemoryStream(bytes);
        }

        public void WriteContent(byte[] content)
        {
            throw new NotSupportedException();
        }

        public void Delete()
        {
            throw new NotSupportedException();
        }
    }
}