#if NET45

using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.DependencyInjection.NestedProviders;
using Moq;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.Core.Test
{
    public class ActionAttributeTests
    {
        private DefaultActionDiscoveryConventions _actionDiscoveryConventions = new DefaultActionDiscoveryConventions();
        private IControllerDescriptorFactory _controllerDescriptorFactory = new DefaultControllerDescriptorFactory();
        private IParameterDescriptorFactory _parameterDescriptorFactory = new DefaultParameterDescriptorFactory();
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
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "HttpMethodAttributeTests_RestOnly" },
                                                { "action", "Patch" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

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
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "HttpMethodAttributeTests_RestOnly" },
                                                { "action", "Put" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

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
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "HttpMethodAttributeTests_RestOnly" },
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

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
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "HttpMethodAttributeTests_DefaultMethodValidation" },
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal("Index", result.Name);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RequestContext context)
        {
            return await InvokeActionSelector(context, _actionDiscoveryConventions);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RequestContext context, DefaultActionDiscoveryConventions actionDiscoveryConventions)
        {
            var actionDescriptorProvider = GetActionDescriptorProvider(actionDiscoveryConventions);
            var descriptorProvider =
                new NestedProviderManager<ActionDescriptorProviderContext>(new[] { actionDescriptorProvider });
            var bindingProvider = new Mock<IActionBindingContextProvider>();

            var defaultActionSelector = new DefaultActionSelector(descriptorProvider, bindingProvider.Object);
            return await defaultActionSelector.SelectAsync(context);
        }

        private ReflectedActionDescriptorProvider GetActionDescriptorProvider(DefaultActionDiscoveryConventions actionDiscoveryConventions)
        {
            var controllerAssemblyProvider = new Mock<IControllerAssemblyProvider>();
            controllerAssemblyProvider.SetupGet(x => x.CandidateAssemblies).Returns(_controllerAssemblies);
            return new ReflectedActionDescriptorProvider(
                                        controllerAssemblyProvider.Object,
                                        actionDiscoveryConventions,
                                        _controllerDescriptorFactory,
                                        _parameterDescriptorFactory,
                                        null);
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