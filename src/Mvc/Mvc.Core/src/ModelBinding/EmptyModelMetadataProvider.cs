// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public class EmptyModelMetadataProvider : DefaultModelMetadataProvider
    {
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