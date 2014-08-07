// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using System.Collections.Generic;

namespace Microsoft.AspNet.WebUtilities.Collections
{
    /// <summary>
    /// Contains the parsed form values.
    /// </summary>
    public class FormCollection : ReadableStringCollection, IFormCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Microsoft.AspNet.WebUtilities.FormCollection" /> class.
        /// </summary>
        /// <param name="store">The store for the form.</param>
        public FormCollection(IDictionary<string, string[]> store)
            : base(store)
        {
        }
    }
}
