// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Http.Internal
{
    public class FormFileCollection : List<IFormFile>, IFormFileCollection
    {
        public IFormFile this[string name] => GetFile(name);

        public IFormFile GetFile(string name)
        {
            foreach (var file in this)
            {
                if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            return null;
        }

        public IReadOnlyList<IFormFile> GetFiles(string name)
        {
            var files = new List<IFormFile>();

            foreach (var file in this)
            {
                if (string.Equals(name, file.Name, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(file);
                }
            }

            return files;
        }
    }
}