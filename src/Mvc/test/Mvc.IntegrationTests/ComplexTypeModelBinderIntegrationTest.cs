// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

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
