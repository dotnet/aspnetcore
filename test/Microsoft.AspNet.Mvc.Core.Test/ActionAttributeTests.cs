// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if NET45

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.NestedProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ActionAttributeTests
    {
        private DefaultActionDiscoveryConventions _actionDiscoveryConventions = new DefaultActionDiscoveryConventions();
        private IEnumerable<Assembly> _controllerAssemblies = new[] { Assembly.GetExecutingAssembly() };

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task HttpMethodAttribute_ActionWithMultipleHttpMethodAttributeViaAcceptVerbs_ORsMultipleHttpMethods(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "HttpMethodAttributeTests_RestOnly" },
                { "action", "Patch" }
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Patch", result.Name);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task HttpMethodAttribute_ActionWithMultipleHttpMethodAttributes_ORsMultipleHttpMethods(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>()
            {
                { "controller", "HttpMethodAttributeTests_RestOnly" },
                { "action", "Put" }
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Put", result.Name);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        public async Task HttpMethodAttribute_ActionDecoratedWithHttpMethodAttribute_OverridesConvention(string verb)
        {
            // Arrange
            // Note no action name is passed, hence should return a null action descriptor.
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>()
            {
                { "controller", "HttpMethodAttributeTests_RestOnly" },
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal(null, result);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task HttpMethodAttribute_DefaultMethod_IgnoresMethodsWithCustomAttributesAndInvalidMethods(string verb)
        {
            // Arrange
            // Note no action name is passed, hence should return a null action descriptor.
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "HttpMethodAttributeTests_DefaultMethodValidation" },
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Index", result.Name);
        }

        [Theory]
        [InlineData("Put")]
        [InlineData("RPCMethod")]
        [InlineData("RPCMethodWithHttpGet")]
        public void NonActionAttribute_ActionNotReachable(string actionName)
        {
            // Arrange
            var actionDescriptorProvider = GetActionDescriptorProvider(_actionDiscoveryConventions);

            // Act
            var result = actionDescriptorProvider.GetDescriptors()
                                                 .Select(x => x as ReflectedActionDescriptor)
                                                 .FirstOrDefault(
                                                            x=> x.ControllerName == "NonAction" &&
                                                                x.Name == actionName);

            // Assert
            Assert.Null(result);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("POST")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task ActionNameAttribute_ActionGetsExposedViaActionName_UnreachableByConvention(string verb)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "ActionName" },
                { "action", "RPCMethodWithHttpGet" }
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal(null, result);
        }

        [Theory]
        [InlineData("GET", "CustomActionName_Verb")]
        [InlineData("PUT", "CustomActionName_Verb")]
        [InlineData("POST", "CustomActionName_Verb")]
        [InlineData("DELETE", "CustomActionName_Verb")]
        [InlineData("PATCH", "CustomActionName_Verb")]
        [InlineData("GET", "CustomActionName_DefaultMethod")]
        [InlineData("PUT", "CustomActionName_DefaultMethod")]
        [InlineData("POST", "CustomActionName_DefaultMethod")]
        [InlineData("DELETE", "CustomActionName_DefaultMethod")]
        [InlineData("PATCH", "CustomActionName_DefaultMethod")]
        [InlineData("GET", "CustomActionName_RpcMethod")]
        [InlineData("PUT", "CustomActionName_RpcMethod")]
        [InlineData("POST", "CustomActionName_RpcMethod")]
        [InlineData("DELETE", "CustomActionName_RpcMethod")]
        [InlineData("PATCH", "CustomActionName_RpcMethod")]
        public async Task ActionNameAttribute_DifferentActionName_UsesActionNameFromActionNameAttribute(string verb, string actionName)
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext(verb));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "ActionName" },
                { "action", actionName }
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal(actionName, result.Name);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RouteContext context)
        {
            return await InvokeActionSelector(context, _actionDiscoveryConventions);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RouteContext context, 
                                                                  DefaultActionDiscoveryConventions actionDiscoveryConventions)
        {
            var actionDescriptorProvider = GetActionDescriptorProvider(actionDiscoveryConventions);
            var descriptorProvider =
                new NestedProviderManager<ActionDescriptorProviderContext>(new[] { actionDescriptorProvider });

            var serviceContainer = new ServiceContainer();
            serviceContainer.AddService(typeof(INestedProviderManager<ActionDescriptorProviderContext>),
                                        descriptorProvider);

            var actionCollectionDescriptorProvider = new DefaultActionDescriptorsCollectionProvider(serviceContainer);

            var bindingProvider = new Mock<IActionBindingContextProvider>();

            var defaultActionSelector = new DefaultActionSelector(actionCollectionDescriptorProvider,
                                                                  bindingProvider.Object);

            return await defaultActionSelector.SelectAsync(context);
        }

        private ReflectedActionDescriptorProvider GetActionDescriptorProvider(DefaultActionDiscoveryConventions actionDiscoveryConventions)
        {
            var controllerAssemblyProvider = new Mock<IControllerAssemblyProvider>();
            controllerAssemblyProvider.SetupGet(x => x.CandidateAssemblies).Returns(_controllerAssemblies);

            return new ReflectedActionDescriptorProvider(
                                        controllerAssemblyProvider.Object,
                                        actionDiscoveryConventions,
                                        null,
                                        new MockMvcOptionsAccessor());
        }

        private static HttpContext GetHttpContext(string httpMethod)
        {
            var request = new Mock<HttpRequest>();
            var headers = new Mock<IHeaderDictionary>();
            request.SetupGet(r => r.Headers).Returns(headers.Object);
            request.SetupGet(x => x.Method).Returns(httpMethod);
            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(c => c.Request).Returns(request.Object);
            return httpContext.Object;
        }

        private class CustomActionConvention : DefaultActionDiscoveryConventions
        {
            public override IEnumerable<string> GetSupportedHttpMethods(MethodInfo methodInfo)
            {
                if (methodInfo.Name.Equals("PostSomething", StringComparison.OrdinalIgnoreCase))
                {
                    return new[] { "POST" };
                }

                return null;
            }
        }

        #region Controller Classes

        private class NonActionController
        {
            [NonAction]
            public void Put()
            {
            }

            [NonAction]
            public void RPCMethod()
            {
            }

            [NonAction]
            [HttpGet]
            public void RPCMethodWithHttpGet()
            {
            }
        }

        private class HttpMethodAttributeTests_DefaultMethodValidationController
        {
            public void Index()
            {
            }

            // Method with custom attribute.
            [HttpGet]
            public void Get()
            { }

            // InvalidMethod ( since its private)
            private void Post()
            { }
        }

        private class ActionNameController
        {
            [ActionName("CustomActionName_Verb")]
            public void Put()
            {
            }

            [ActionName("CustomActionName_DefaultMethod")]
            public void Index()
            {
            }

            [ActionName("CustomActionName_RpcMethod")]
            public void RPCMethodWithHttpGet()
            {
            }
        }

        private class HttpMethodAttributeTests_RestOnlyController
        {
            [HttpGet]
            [HttpPut]
            [HttpPost]
            [HttpDelete]
            [HttpPatch]
            public void Put()
            {
            }

            [AcceptVerbs("PUT", "post", "GET", "delete", "pATcH")]
            public void Patch()
            {
            }
        }

        private class HttpMethodAttributeTests_DerivedController : HttpMethodAttributeTests_RestOnlyController
        {
        }

        #endregion Controller Classes
    }
}

#endif