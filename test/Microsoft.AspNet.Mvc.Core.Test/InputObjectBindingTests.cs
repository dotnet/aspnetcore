// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.OptionDescriptors;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class InputObjectBindingTests
    {
        private static ControllerActionInvoker GetControllerActionInvoker(
            string input, Type parameterType, IInputFormatter selectedFormatter, string contentType)
        {
            var mvcOptions = new MvcOptions();
            var setup = new MvcOptionsSetup();

            setup.Configure(mvcOptions);
            var accessor = new Mock<IOptions<MvcOptions>>();
            accessor.SetupGet(a => a.Options)
                    .Returns(mvcOptions);
            var validatorProvider = new DefaultModelValidatorProviderProvider(
                accessor.Object, Mock.Of<ITypeActivator>(), Mock.Of<IServiceProvider>());

            Func<object, int> method = x => 1;
            var actionDescriptor = new ControllerActionDescriptor
            {
                MethodInfo = method.Method,
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor
                    {
                        BinderMetadata = new FromBodyAttribute(),
                        Name = "foo",
                        ParameterType = parameterType,
                    }
                }
            };

            var metadataProvider = new EmptyModelMetadataProvider();
            var actionContext = GetActionContext(
                Encodings.UTF8EncodingWithoutBOM.GetBytes(input), actionDescriptor, contentType);

            var inputFormatterSelector = new Mock<IInputFormatterSelector>();
            inputFormatterSelector.Setup(a => a.SelectFormatter(It.IsAny<InputFormatterContext>()))
                .Returns(selectedFormatter);
            var bindingContext = new ActionBindingContext(actionContext,
                                                          metadataProvider,
                                                          Mock.Of<IModelBinder>(),
                                                          Mock.Of<IValueProvider>(),
                                                          inputFormatterSelector.Object,
                                                          new CompositeModelValidatorProvider(validatorProvider));

            var actionBindingContextProvider = new Mock<IActionBindingContextProvider>();
            actionBindingContextProvider.Setup(p => p.GetActionBindingContextAsync(It.IsAny<ActionContext>()))
                                        .Returns(Task.FromResult(bindingContext));

            var inputFormattersProvider = new Mock<IInputFormattersProvider>();
            inputFormattersProvider.SetupGet(o => o.InputFormatters)
                                            .Returns(new List<IInputFormatter>());
            return new ControllerActionInvoker(actionContext,
                                                     Mock.Of<INestedProviderManager<FilterProviderContext>>(),
                                                     Mock.Of<IControllerFactory>(),
                                                     actionDescriptor,
                                                     inputFormattersProvider.Object,
                                                     Mock.Of<IControllerActionArgumentBinder>());
        }

        private static ActionContext GetActionContext(byte[] contentBytes,
                                                      ActionDescriptor actionDescriptor,
                                                      string contentType)
        {
            return new ActionContext(GetHttpContext(contentBytes, contentType),
                                     new RouteData(),
                                     actionDescriptor);
        }

        private static HttpContext GetHttpContext(byte[] contentBytes,
                                                  string contentType)
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(f => f.Body).Returns(new MemoryStream(contentBytes));
            request.SetupGet(f => f.ContentType).Returns(contentType);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }
    }

    public class Person
    {
        public string Name { get; set; }
    }

    public class User : Person
    {
        [MinLength(5)]
        public string UserName { get; set; }
    }

    public class Customers
    {
        [Required]
        public List<User> Users { get; set; }
    }
}