// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Description;
using Microsoft.AspNet.Mvc.HeaderValueAbstractions;

namespace ApiExplorer
{
    public class ProducesTypeAttribute : ResultFilterAttribute, IApiResponseMetadataProvider
    {
        public ProducesTypeAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; private set; }

        public void SetContentTypes(IList<MediaTypeHeaderValue> contentTypes)
        {
        }
    }
}