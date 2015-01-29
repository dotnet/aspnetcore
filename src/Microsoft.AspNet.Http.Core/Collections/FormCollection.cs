// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Http.Core.Collections
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : ReadableStringCollection, IFormCollection
    {
        public FormCollection([NotNull] IDictionary<string, string[]> store)
            : this(store, new FormFileCollection())
        {
        }

        public FormCollection([NotNull] IDictionary<string, string[]> store, [NotNull] IFormFileCollection files)
            : base(store)
        {
            Files = files;
        }

        public IFormFileCollection Files { get; private set; }
    }
}
