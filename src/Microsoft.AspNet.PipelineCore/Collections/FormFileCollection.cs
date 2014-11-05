// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.PipelineCore.Collections
{
    public class FormFileCollection : List<IFormFile>, IFormFileCollection
    {
        public IFormFile this[string name]
        {
            get { return GetFile(name); }
        }

        public IFormFile GetFile(string name)
        {
            return Find(file => string.Equals(name, GetName(file.ContentDisposition)));
        }

        public IList<IFormFile> GetFiles(string name)
        {
            return FindAll(file => string.Equals(name, GetName(file.ContentDisposition)));
        }

        private static string GetName(string contentDisposition)
        {
            // TODO: Strongly typed headers will take care of this
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            var offset = contentDisposition.IndexOf("name=\"") + "name=\"".Length;
            var key = contentDisposition.Substring(offset, contentDisposition.IndexOf("\"", offset) - offset); // Remove quotes
            return key;
        }
    }
}