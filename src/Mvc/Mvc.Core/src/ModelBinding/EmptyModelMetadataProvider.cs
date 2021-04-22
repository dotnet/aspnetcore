// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// A <see cref="DefaultBindingMetadataProvider"/> that represents an empty model.
    /// </summary>
    public class EmptyModelMetadataProvider : DefaultModelMetadataProvider
    {
        /// <summary>
        /// Initializes a new <see cref="EmptyModelMetadataProvider"/>.
        /// </summary>
        public EmptyModelMetadataProvider()
            : base(
                  new DefaultCompositeMetadataDetailsProvider(new List<IMetadataDetailsProvider>()),
                  new OptionsAccessor())
        {
        }

        private class OptionsAccessor : IOptions<MvcOptions>
        {
            public MvcOptions Value { get; } = new MvcOptions();
        }
    }
}
