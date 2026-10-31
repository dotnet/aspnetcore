// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Mvc.IntegrationTests;

public class TestMvcOptions : IOptions<MvcOptions>
{
    public TestMvcOptions()
    {
        Value = new MvcOptions();
        var optionsSetup = new MvcCoreMvcOptionsSetup(new TestHttpRequestStreamReaderFactory());
        optionsSetup.Configure(Value);

        var validationAttributeAdapterProvider = new ValidationAttributeAdapterProvider();
        var dataAnnotationLocalizationOptions = Options.Create(new MvcDataAnnotationsLocalizationOptions());
        var stringLocalizer = new Mock<IStringLocalizer>();
        var stringLocalizerFactory = new Mock<IStringLocalizerFactory>();
        stringLocalizerFactory
            .Setup(s => s.Create(It.IsAny<Type>()))
            .Returns(stringLocalizer.Object);

        var dataAnnotationOptionsSetup = new MvcDataAnnotationsMvcOptionsSetup(
            validationAttributeAdapterProvider,
            dataAnnotationLocalizationOptions,
            stringLocalizerFactory.Object);
        dataAnnotationOptionsSetup.Configure(Value);

        var loggerFactory = new LoggerFactory();
        var jsonOptions = Options.Create(new MvcNewtonsoftJsonOptions());
        var charPool = ArrayPool<char>.Shared;
        var objectPoolProvider = new DefaultObjectPoolProvider();

        var mvcJsonMvcOptionsSetup = new NewtonsoftJsonMvcOptionsSetup(
            loggerFactory,
            jsonOptions,
            charPool,
            objectPoolProvider);
        mvcJsonMvcOptionsSetup.Configure(Value);
    }

    public MvcOptions Value { get; }
}
