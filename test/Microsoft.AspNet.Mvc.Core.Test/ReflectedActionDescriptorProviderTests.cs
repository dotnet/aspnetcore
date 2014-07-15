// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Routing;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ReflectedActionDescriptorProviderTests
    {
        [Fact]
        public void GetDescriptors_GetsDescriptorsOnlyForValidActions()
        {
            // Arrange
            var provider = GetProvider(typeof(PersonController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var actionNames = descriptors.Select(ad => ad.Name);

            // Assert
            Assert.Equal(new[] { "GetPerson", "ListPeople", }, actionNames);
        }

        [Fact]
        public void GetDescriptors_IncludesFilters()
        {
            // Arrange
            var globalFilter = new MyFilterAttribute(1);
            var provider = GetProvider(typeof(FiltersController).GetTypeInfo(), new IFilter[]
            {
                globalFilter,
            });

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal(3, descriptor.FilterDescriptors.Count);

            var filter1 = descriptor.FilterDescriptors[2];
            Assert.Same(globalFilter, filter1.Filter);
            Assert.Equal(FilterScope.Global, filter1.Scope);

            var filter2 = descriptor.FilterDescriptors[1];
            Assert.Equal(2, Assert.IsType<MyFilterAttribute>(filter2.Filter).Value);
            Assert.Equal(FilterScope.Controller, filter2.Scope);

            var filter3 = descriptor.FilterDescriptors[0];
            Assert.Equal(3, Assert.IsType<MyFilterAttribute>(filter3.Filter).Value); ;
            Assert.Equal(FilterScope.Action, filter3.Scope);
        }

        [Fact]
        public void GetDescriptors_AddsHttpMethodConstraints()
        {
            // Arrange
            var provider = GetProvider(typeof(HttpMethodController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var descriptor = Assert.Single(descriptors);

            // Assert
            Assert.Equal("OnlyPost", descriptor.Name);

            Assert.Single(descriptor.MethodConstraints);
            Assert.Equal(new string[] { "POST" }, descriptor.MethodConstraints[0].HttpMethods);
        }

        [Fact]
        public void GetDescriptors_WithRouteDataConstraint_WithBlockNonAttributedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(HttpMethodController).GetTypeInfo(),
                typeof(BlockNonAttributedActionsController).GetTypeInfo()).ToArray();

            var descriptorWithoutConstraint = Assert.Single(
                descriptors, 
                ad => ad.RouteConstraints.Any(
                    c => c.RouteKey == "key" && c.KeyHandling == RouteKeyHandling.DenyKey));

            var descriptorWithConstraint = Assert.Single(
                descriptors,
                ad => ad.RouteConstraints.Any(
                    c => 
                        c.KeyHandling == RouteKeyHandling.RequireKey && 
                        c.RouteKey == "key" && 
                        c.RouteValue == "value"));

            // Assert
            Assert.Equal(2, descriptors.Length);

            Assert.Equal(3, descriptorWithConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "BlockNonAttributedActions");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "Edit");

            Assert.Equal(3, descriptorWithoutConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "HttpMethod");
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "OnlyPost");
        }

        [Fact]
        public void GetDescriptors_WithRouteDataConstraint_WithoutBlockNonAttributedActions()
        {
            // Arrange & Act
            var descriptors = GetDescriptors(
                typeof(HttpMethodController).GetTypeInfo(),
                typeof(DontBlockNonAttributedActionsController).GetTypeInfo()).ToArray();

            var descriptorWithConstraint = Assert.Single(
                descriptors,
                ad => ad.RouteConstraints.Any(
                    c =>
                        c.KeyHandling == RouteKeyHandling.RequireKey &&
                        c.RouteKey == "key" &&
                        c.RouteValue == "value"));

            var descriptorWithoutConstraint = Assert.Single(
                descriptors,
                ad => !ad.RouteConstraints.Any(c => c.RouteKey == "key"));

            // Assert
            Assert.Equal(2, descriptors.Length);

            Assert.Equal(3, descriptorWithConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "DontBlockNonAttributedActions");
            Assert.Single(
                descriptorWithConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "Create");

            Assert.Equal(2, descriptorWithoutConstraint.RouteConstraints.Count);
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "controller" &&
                    c.RouteValue == "HttpMethod");
            Assert.Single(
                descriptorWithoutConstraint.RouteConstraints,
                c =>
                    c.RouteKey == "action" &&
                    c.RouteValue == "OnlyPost");
        }

        [Fact]
        public void BuildModel_IncludesGlobalFilters()
        {
            // Arrange
            var filter = new MyFilterAttribute(1);
            var provider = GetProvider(typeof(PersonController).GetTypeInfo(), new IFilter[]
            {
                filter,
            });

            // Act
            var model = provider.BuildModel();

            // Assert
            var filters = model.Filters;
            Assert.Same(filter, Assert.Single(filters));
        }

        private ReflectedActionDescriptorProvider GetProvider(
            TypeInfo controllerTypeInfo, 
            IEnumerable<IFilter> filters = null)
        {
            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfo);

            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new Assembly[] { controllerTypeInfo.Assembly });

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                filters,
                new MockMvcOptionsAccessor(),
                Mock.Of<IInlineConstraintResolver>());

            return provider;
        }

        private IEnumerable<ActionDescriptor> GetDescriptors(params TypeInfo[] controllerTypeInfos)
        {
            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfos);

            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(controllerTypeInfos.Select(cti => cti.Assembly).Distinct());

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                null,
                new MockMvcOptionsAccessor(),
                null);
            
            return provider.GetDescriptors();
        }

        private class HttpMethodController
        {
            [HttpPost]
            public void OnlyPost()
            {
            }
        }

        private class PersonController
        {
            public void GetPerson()
            { }

            public void ListPeople()
            { }

            [NonAction]
            public void NotAnAction()
            { }
        }

        public class MyRouteConstraintAttribute : RouteConstraintAttribute
        {
            public MyRouteConstraintAttribute(bool blockNonAttributedActions)
                : base("key", "value", blockNonAttributedActions)
            {
            }
        }

        [MyRouteConstraintAttribute(blockNonAttributedActions: true)]
        private class BlockNonAttributedActionsController
        {
            public void Edit()
            {
            }
        }

        [MyRouteConstraintAttribute(blockNonAttributedActions: false)]
        private class DontBlockNonAttributedActionsController
        {
            public void Create()
            {
            }
        }

        private class MyFilterAttribute : Attribute, IFilter
        {
            public MyFilterAttribute(int value)
            {
                Value = value;
            }

            public int Value { get; private set; }
        }

        [MyFilter(2)]
        private class FiltersController
        {
            [MyFilter(3)]
            public void FilterAction()
            {
            }
        }
    }
}
