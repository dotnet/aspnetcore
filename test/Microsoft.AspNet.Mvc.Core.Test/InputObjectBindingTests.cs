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
        [Fact]
        public async Task GetArguments_UsingInputFormatter_DeserializesWithoutErrors_WhenValidationAttributesAreAbsent()
        {
            // Arrange
            var sampleName = "SampleName";
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<Person><Name>" + sampleName + "</Name></Person>";
            var modelStateDictionary = new ModelStateDictionary();
            var invoker = GetControllerActionInvoker(
                input, typeof(Person), new XmlSerializerInputFormatter(), "application/xml");

            // Act
            var result = await invoker.GetActionArguments(modelStateDictionary);

            // Assert
            Assert.True(modelStateDictionary.IsValid);
            Assert.Equal(0, modelStateDictionary.ErrorCount);
            var model = result["foo"] as Person;
            Assert.Equal(sampleName, model.Name);
        }

        [Fact]
        public async Task GetArguments_UsingInputFormatter_DeserializesWithValidationError()
        {
            // Arrange
            var sampleName = "SampleName";
            var sampleUserName = "No5";
            var input = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                            "<User><Name>" + sampleName + "</Name><UserName>" + sampleUserName + "</UserName></User>";
            var modelStateDictionary = new ModelStateDictionary();
            var invoker = GetControllerActionInvoker(input, typeof(User), new XmlSerializerInputFormatter(), "application/xml");

            // Act
            var result = await invoker.GetActionArguments(modelStateDictionary);

            // Assert
            Assert.False(modelStateDictionary.IsValid);
            Assert.Equal(1, modelStateDictionary.ErrorCount);
            Assert.Equal(
                ValidationAttributeUtil.GetMinLengthErrorMessage(5, "UserName"),
                Assert.Single(Assert.Single(modelStateDictionary.Values).Errors).ErrorMessage);
            var model = result["foo"] as User;
            Assert.Equal(sampleName, model.Name);
            Assert.Equal(sampleUserName, model.UserName);
        }

        [Fact]
        public async Task GetArguments_UsingInputFormatter_DeserializesArrays()
        {
            // Arrange
            var sampleFirstUser = "FirstUser";
            var sampleFirstUserName = "fuser";
            var sampleSecondUser = "SecondUser";
            var sampleSecondUserName = "suser";
            var input = "{'Users': [{Name : '" + sampleFirstUser + "', UserName: '" + sampleFirstUserName +
                "'}, {Name: '" + sampleSecondUser + "', UserName: '" + sampleSecondUserName + "'}]}";
            var modelStateDictionary = new ModelStateDictionary();
            var invoker = GetControllerActionInvoker(input, typeof(Customers), new JsonInputFormatter(), "application/xml");

            // Act
            var result = await invoker.GetActionArguments(modelStateDictionary);

            // Assert
            Assert.True(modelStateDictionary.IsValid);
            Assert.Equal(0, modelStateDictionary.ErrorCount);
            var model = result["foo"] as Customers;
            Assert.Equal(2, model.Users.Count);
            Assert.Equal(sampleFirstUser, model.Users[0].Name);
            Assert.Equal(sampleFirstUserName, model.Users[0].UserName);
            Assert.Equal(sampleSecondUser, model.Users[1].Name);
            Assert.Equal(sampleSecondUserName, model.Users[1].UserName);
        }

        [Fact]
        public async Task GetArguments_UsingInputFormatter_DeserializesArrays_WithErrors()
        {
            // Arrange
            var sampleFirstUser = "FirstUser";
            var sampleFirstUserName = "fusr";
            var sampleSecondUser = "SecondUser";
            var sampleSecondUserName = "susr";
            var input = "{'Users': [{Name : '" + sampleFirstUser + "', UserName: '" + sampleFirstUserName +
                "'}, {Name: '" + sampleSecondUser + "', UserName: '" + sampleSecondUserName + "'}]}";
            var modelStateDictionary = new ModelStateDictionary();
            var invoker = GetControllerActionInvoker(input, typeof(Customers), new JsonInputFormatter(), "application/xml");

            // Act
            var result = await invoker.GetActionArguments(modelStateDictionary);

            // Assert
            Assert.False(modelStateDictionary.IsValid);
            Assert.Equal(2, modelStateDictionary.ErrorCount);
            var model = result["foo"] as Customers;
            Assert.Equal(
                ValidationAttributeUtil.GetMinLengthErrorMessage(5, "UserName"),
                modelStateDictionary["foo.Users[0].UserName"].Errors[0].ErrorMessage);
            Assert.Equal(
                ValidationAttributeUtil.GetMinLengthErrorMessage(5, "UserName"),
                modelStateDictionary["foo.Users[1].UserName"].Errors[0].ErrorMessage);
            Assert.Equal(2, model.Users.Count);
            Assert.Equal(sampleFirstUser, model.Users[0].Name);
            Assert.Equal(sampleFirstUserName, model.Users[0].UserName);
            Assert.Equal(sampleSecondUser, model.Users[1].Name);
            Assert.Equal(sampleSecondUserName, model.Users[1].UserName);
        }

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
                                    Name = "foo",
                                    BodyParameterInfo = new BodyParameterInfo(parameterType)
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
                                                     actionBindingContextProvider.Object,
                                                     Mock.Of<INestedProviderManager<FilterProviderContext>>(),
                                                     Mock.Of<IControllerFactory>(),
                                                     actionDescriptor,
                                                     inputFormattersProvider.Object,
                                                     new DefaultBodyModelValidator());
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