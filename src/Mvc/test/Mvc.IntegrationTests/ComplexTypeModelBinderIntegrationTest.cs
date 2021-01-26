// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests
{

    public class ComplexTypeModelBinderIntegrationTest : ComplexTypeIntegrationTestBase
    {
#pragma warning disable CS0618 // Type or member is obsolete
        protected override Type ExpectedModelBinderType => typeof(ComplexTypeModelBinder);

        protected override ModelBindingTestContext GetTestContext(
            Action<HttpRequest> updateRequest = null,
            Action<MvcOptions> updateOptions = null,
            IModelMetadataProvider metadataProvider = null)
        {
            return ModelBindingTestHelper.GetTestContext(
                updateRequest,
                updateOptions: options =>
                {
                    options.ModelBinderProviders.RemoveType<ComplexObjectModelBinderProvider>();
                    options.ModelBinderProviders.Add(new ComplexTypeModelBinderProvider());

                    updateOptions?.Invoke(options);
                },
                metadataProvider: metadataProvider);
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
