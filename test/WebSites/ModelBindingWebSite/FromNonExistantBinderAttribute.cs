// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace ModelBindingWebSite
{
    public class FromNonExistantBinderAttribute : Attribute, IBindingSourceMetadata
    {
        public BindingSource BindingSource
        {
            get
            {
                return BindingSource.Custom;
            }
        }
    }
}