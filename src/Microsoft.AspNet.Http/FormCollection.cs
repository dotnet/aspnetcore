// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.Http.Internal
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : ReadableStringCollection, IFormCollection
    {
        public FormCollection([NotNull] IDictionary<string, StringValues> store)
            : this(store, new FormFileCollection())
        {
        }

        public FormCollection([NotNull] IDictionary<string, StringValues> store, [NotNull] IFormFileCollection files)
            : base(store)
        {
            Files = files;
        }

        public IFormFileCollection Files { get; private set; }
    }
}
