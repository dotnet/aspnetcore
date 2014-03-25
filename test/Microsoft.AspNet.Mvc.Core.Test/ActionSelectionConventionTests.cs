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
    public class ActionSelectionConventionTests
    {
        private IActionDiscoveryConventions _actionDiscoveryConventions = new DefaultActionDiscoveryConventions();
        private IControllerDescriptorFactory _controllerDescriptorFactory = new DefaultControllerDescriptorFactory();
        private IParameterDescriptorFactory _parameterDescriptorFactory = new DefaultParameterDescriptorFactory();
        private IEnumerable<Assembly> _controllerAssemblies = new[] { Assembly.GetExecutingAssembly() };

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task ActionSelection_IndexSelectedByDefaultInAbsenceOfVerbOnlyMethod(string verb)
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "RpcOnly" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal("Index", result.Name);
        }

        [Theory]
        [InlineData("GET")]
        [InlineData("POST")]
        public async Task ActionSelection_PrefersVerbOnlyMethodOverIndex(string verb)
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "MixedRpcAndRest" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal(verb, result.Name, StringComparer.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task ActionSelection_IndexNotSelectedByDefaultExceptGetAndPostVerbs(string verb)
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "RpcOnly" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal(null, result);
        }

        [Theory]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public async Task ActionSelection_NoConventionBasedRoutingForHeadAndOptions(string verb)
        {
            // Arrange
            var requestContext = new RequestContext(
                GetHttpContext(verb),
                new Dictionary<string, object>
                {
                    {"controller", "MixedRpcAndRest"},
                });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal(null, result);
        }

        [Theory]
        [InlineData("HEAD")]
        [InlineData("OPTIONS")]
        public async Task ActionSelection_ActionNameBasedRoutingForHeadAndOptions(string verb)
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext(verb),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "MixedRpcAndRest" },
                                                { "action", verb },
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext);

            // Assert
            Assert.Equal(verb, result.Name, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ActionSelection_ChangeDefaultConventionPicksCustomMethodForPost_DefaultMethodIsSelectedForGet()
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext("GET"),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "RpcOnly" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext, new CustomActionConvention());

            // Assert
            Assert.Equal("INDEX", result.Name, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ActionSelection_ChangeDefaultConventionPicksCustomMethodForPost_CutomMethodIsSelected()
        {
            // Arrange
            var requestContext = new RequestContext(
                                        GetHttpContext("POST"),
                                        new Dictionary<string, object>
                                            {
                                                { "controller", "RpcOnly" }
                                            });

            // Act
            var result = await InvokeActionSelector(requestContext, new CustomActionConvention());

            // Assert
            Assert.Equal("PostSomething", result.Name);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RequestContext context)
        {
            return await InvokeActionSelector(context, _actionDiscoveryConventions);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RequestContext context, IActionDiscoveryConventions actionDiscoveryConventions)
        {
            var actionDescriptorProvider = GetActionDescriptorProvider(actionDiscoveryConventions);
            var descriptorProvider =
                new NestedProviderManager<ActionDescriptorProviderContext>(new[] { actionDescriptorProvider });
            var bindingProvider = new Mock<IActionBindingContextProvider>();

            var defaultActionSelector = new DefaultActionSelector(descriptorProvider, bindingProvider.Object);
            return await defaultActionSelector.SelectAsync(context);
        }

        private ReflectedActionDescriptorProvider GetActionDescriptorProvider(IActionDiscoveryConventions actionDiscoveryConventions)
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

        private class MixedRpcAndRestController
        {
            public void Index()
            {
            }

            public void Get()
            {
            }

            public void Post()
            { }

            public void GetSomething()
            { }

            // This will be treated as an RPC method.
            public void Head()
            {
            }

            // This will be treated as an RPC method.
            public void Options()
            {
            }
        }

        private class RestOnlyController
        {
            public void Get()
            {
            }

            public void Put()
            {
            }

            public void Post()
            {
            }

            public void Delete()
            {
            }

            public void Patch()
            {
            }
        }

        private class RpcOnlyController
        {
            public void Index()
            {
            }

            public void GetSomething()
            {
            }

            public void PutSomething()
            {
            }

            public void PostSomething()
            {
            }

            public void DeleteSomething()
            {
            }

            public void PatchSomething()
            {
            }
        }

        private class AmbiguousController
        {
            public void Index(int i)
            { }

            public void Index(string s)
            { }
        }

        #endregion Controller Classes
    }
}

#endif