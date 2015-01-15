// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNet.Http.Core.Collections
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
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            ContentDispositionHeaderValue cd;
            ContentDispositionHeaderValue.TryParse(contentDisposition, out cd);
            return HeaderUtilities.RemoveQuotes(cd?.Name);
        }
    }
}