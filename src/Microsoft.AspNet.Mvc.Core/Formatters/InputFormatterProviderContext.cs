// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class InputFormatterProviderContext
    {
        public InputFormatterProviderContext([NotNull] ActionContext actionContext,
                                             [NotNull] ModelMetadata metadata, 
                                             [NotNull] ModelStateDictionary modelState)
        {
            ActionContext = actionContext;
            Metadata = metadata;
            ModelState = modelState;
        }

        public ActionContext ActionContext { get; private set; }

        public ModelMetadata Metadata { get; private set; }

        public ModelStateDictionary ModelState { get; private set; }
    }
}
