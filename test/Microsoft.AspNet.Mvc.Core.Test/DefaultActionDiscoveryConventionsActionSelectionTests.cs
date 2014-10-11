// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if ASPNET50

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.Routing;
using Microsoft.AspNet.Routing;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.NestedProviders;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class DefaultActionDiscoveryConventionsActionSelectionTests
    {
        [Fact]
        public async Task ActionSelection_ActionSelectedByName()
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext("GET"));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "RpcOnly" },
                { "action", "Index" }
            };

            // Act
            var result = await InvokeActionSelector(routeContext);

            // Assert
            Assert.Equal("Index", result.Name);
        }

        // Uses custom conventions to map a web-api-style action
        [Fact]
        public async Task ActionSelection_ChangeDefaultConventionPicksCustomMethodForPost_CutomMethodIsSelected()
        {
            // Arrange
            var routeContext = new RouteContext(GetHttpContext("POST"));
            routeContext.RouteData.Values = new Dictionary<string, object>
            {
                { "controller", "RpcOnly" }
            };

            // Act
            var result = await InvokeActionSelector(routeContext, new CustomActionConvention());

            // Assert
            Assert.Equal("PostSomething", result.Name);
        }

        private async Task<ActionDescriptor> InvokeActionSelector(RouteContext context)
        {
            var controllerTypeInfos = typeof(DefaultActionDiscoveryConventionsActionSelectionTests)
                .GetNestedTypes(BindingFlags.NonPublic)
                .Select(ct => ct.GetTypeInfo())
                .ToArray();

            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfos);
            return await InvokeActionSelector(context, conventions);
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
            var decisionTreeProvider = new ActionSelectorDecisionTreeProvider(actionCollectionDescriptorProvider);

            var actionConstraintProvider = new NestedProviderManager<ActionConstraintProviderContext>(
                new INestedProvider<ActionConstraintProviderContext>[]
            {
                new DefaultActionConstraintProvider(serviceContainer),
            });

            var defaultActionSelector = new DefaultActionSelector(
                actionCollectionDescriptorProvider,
                decisionTreeProvider,
                actionConstraintProvider,
                NullLoggerFactory.Instance);

            return await defaultActionSelector.SelectAsync(context);
        }

        private ControllerActionDescriptorProvider GetActionDescriptorProvider(DefaultActionDiscoveryConventions actionDiscoveryConventions)
        {
            var assemblies = new Assembly[] { typeof(DefaultActionDiscoveryConventionsActionSelectionTests).GetTypeInfo().Assembly, };
            var AssemblyProvider = new Mock<IAssemblyProvider>();
            AssemblyProvider.SetupGet(x => x.CandidateAssemblies).Returns(assemblies);
            return new ControllerActionDescriptorProvider(
                                        AssemblyProvider.Object,
                                        actionDiscoveryConventions,
                                        new TestGlobalFilterProvider(),
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
            public override bool IsController([NotNull]TypeInfo typeInfo)
            {
                return
                    typeof(DefaultActionDiscoveryConventionsActionSelectionTests)
                    .GetNestedTypes(BindingFlags.NonPublic)
                    .Select(ct => ct.GetTypeInfo())
                    .Contains(typeInfo);
            }

            public override IEnumerable<ActionInfo> GetActions([NotNull]MethodInfo methodInfo, [NotNull]TypeInfo controllerTypeInfo)
            {
                var actions = new List<ActionInfo>(
                    base.GetActions(methodInfo, controllerTypeInfo) ?? 
                    new List<ActionInfo>());

                if (methodInfo.Name == "PostSomething")
                {
                    actions[0].HttpMethods = new string[] { "POST" };
                    actions[0].RequireActionNameMatch = false;
                }

                return actions;
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
    }
}

#endif
