// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : ReadableStringCollection, IFormCollection
    {
        public FormCollection(IDictionary<string, StringValues> store)
            : this(store, new FormFileCollection())
        {
        }

        public FormCollection(IDictionary<string, StringValues> store, IFormFileCollection files)
            : base(store)
        {
            if (store == null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (files == null)
            {
                throw new ArgumentNullException(nameof(files));
            }

            Files = files;
        }

        public IFormFileCollection Files { get; }
    }
}
