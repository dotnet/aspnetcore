// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.DependencyInjection.NestedProviders;
using Microsoft.Framework.OptionsModel;
using Moq;
using Xunit;

namespace System.Web.Http
{
    public class ApiControllerActionDiscoveryTest
    {
        // For now we just want to verify that an ApiController is-a controller and produces
        // actions. When we implement the conventions for action discovery, this test will be revised.
        [Fact]
        public void GetActions_ApiControllerWithControllerSuffix_IsController()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            provider.Invoke(context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.ProductsController).GetTypeInfo();
            var filtered = results.Where(ad => ad.ControllerDescriptor.ControllerTypeInfo == controllerType).ToArray();

            Assert.Equal(3, filtered.Length);
        }

        [Fact]
        public void GetActions_ApiControllerWithoutControllerSuffix_IsNotController()
        {
            // Arrange
            var provider = CreateProvider();

            // Act
            var context = new ActionDescriptorProviderContext();
            provider.Invoke(context);

            var results = context.Results.Cast<ControllerActionDescriptor>();

            // Assert
            var controllerType = typeof(TestControllers.Blog).GetTypeInfo();
            var filtered = results.Where(ad => ad.ControllerDescriptor.ControllerTypeInfo == controllerType).ToArray();

            Assert.Empty(filtered);
        }

        private INestedProviderManager<ActionDescriptorProviderContext> CreateProvider()
        {
            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new Assembly[] { typeof(ApiControllerActionDiscoveryTest).Assembly });

            var filterProvider = new Mock<IGlobalFilterProvider>();
            filterProvider
                .SetupGet(fp => fp.Filters)
                .Returns(new List<IFilter>());

            var conventions = new NamespaceLimitedActionDiscoveryConventions();

            var optionsAccessor = new Mock<IOptionsAccessor<MvcOptions>>();
            optionsAccessor
                .SetupGet(o => o.Options)
                .Returns(new MvcOptions());

            var provider = new ControllerActionDescriptorProvider(
                assemblyProvider.Object, 
                conventions,
                filterProvider.Object,
                optionsAccessor.Object);

            return new NestedProviderManager<ActionDescriptorProviderContext>(
                new INestedProvider<ActionDescriptorProviderContext>[]
                {
                    provider
                });
        }

        private class NamespaceLimitedActionDiscoveryConventions : DefaultActionDiscoveryConventions
        {
            public override bool IsController(TypeInfo typeInfo)
            {
                return 
                    typeInfo.Namespace == "System.Web.Http.TestControllers" &&
                    base.IsController(typeInfo);
            }
        }
    }
}

// These need to be public top-level classes to test discovery end-to-end. Don't reuse
// these outside of this test.
namespace System.Web.Http.TestControllers
{
    public class ProductsController : ApiController
    {
        public IActionResult GetAll()
        {
            return null;
        }

        public IActionResult Get(int id)
        {
            return null;
        }

        public IActionResult Edit(int id)
        {
            return null;
        }
    }

    // Not a controller, because there's no controller suffix
    public class Blog : ApiController
    {
        public IActionResult GetBlogPosts()
        {
            return null;
        }
    }
}