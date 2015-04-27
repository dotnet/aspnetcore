// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
using Microsoft.AspNet.Mvc.ModelBinding.Metadata;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class EmptyModelMetadataProvider : DefaultModelMetadataProvider
    {
        public EmptyModelMetadataProvider()
            : base(new DefaultCompositeMetadataDetailsProvider(new IMetadataDetailsProvider[0]))
        {
        }
    }
}