// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Routing;
using Microsoft.AspNet.Mvc.Routing;
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

        [Fact]
        public void GetDescriptor_SetsDisplayName()
        {
            // Arrange
            var provider = GetProvider(typeof(PersonController).GetTypeInfo());

            // Act
            var descriptors = provider.GetDescriptors();
            var displayNames = descriptors.Select(ad => ad.DisplayName);

            // Assert
            Assert.Equal(
                new[]
                {
                    "Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+PersonController.GetPerson",
                    "Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+PersonController.ListPeople",
                },
                displayNames);
        }

        public void AttributeRouting_TokenReplacement_IsAfterReflectedModel()
        {
            // Arrange
            var provider = GetProvider(typeof(TokenReplacementController).GetTypeInfo());

            // Act
            var model = provider.BuildModel();

            // Assert
            var controller = Assert.Single(model.Controllers);
            Assert.Equal("api/Token/[key]/[controller]", controller.RouteTemplate);

            var action = Assert.Single(controller.Actions);
            Assert.Equal("stub/[action]", action.RouteTemplate);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_InActionDescriptor()
        {
            // Arrange
            var provider = GetProvider(typeof(TokenReplacementController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("api/Token/value/TokenReplacement/stub/ThisIsAnAction", action.AttributeRouteTemplate);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_ThrowsWithMultipleMessages()
        {
            // Arrange
            var provider = GetProvider(typeof(MultipleErrorsController).GetTypeInfo());

            var expectedMessage =
                "The following errors occurred with attribute routing information:" + Environment.NewLine +
                Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "MultipleErrorsController.Unknown'" + Environment.NewLine +
                "Error: While processing template 'stub/[action]/[unknown]', a replacement value for the token 'unknown' " +
                "could not be found. Available tokens: 'controller, action'." + Environment.NewLine +
                Environment.NewLine +
                "For action: 'Microsoft.AspNet.Mvc.Test.ReflectedActionDescriptorProviderTests+" +
                "MultipleErrorsController.Invalid'" + Environment.NewLine +
                "Error: The route template '[invalid/syntax' has invalid syntax. A replacement token is not closed.";

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() => { provider.GetDescriptors(); });

            // Assert
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void AttributeRouting_TokenReplacement_CaseInsensitive()
        {
            // Arrange
            var provider = GetProvider(typeof(CaseInsensitiveController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("stub/ThisIsAnAction", action.AttributeRouteTemplate);
        }

        // Token replacement happens before we 'group' routes. So two route templates
        // that are equivalent after token replacement go to the same 'group'.
        [Fact]
        public void AttributeRouting_TokenReplacement_BeforeGroupId()
        {
            // Arrange
            var provider = GetProvider(typeof(SameGroupIdController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors().ToArray();

            var groupIds = actions.Select(
                a => a.RouteConstraints
                    .Where(rc => rc.RouteKey == AttributeRouting.RouteGroupKey)
                    .Select(rc => rc.RouteValue)
                    .Single())
                .ToArray();

            // Assert
            Assert.Equal(2, groupIds.Length);
            Assert.Equal(groupIds[0], groupIds[1]);
        }

        // Parameters are validated later. This action uses the forbidden {action} and {controller}
        [Fact]
        public void AttributeRouting_DoesNotValidateParameters()
        {
            // Arrange
            var provider = GetProvider(typeof(InvalidParametersController).GetTypeInfo());

            // Act
            var actions = provider.GetDescriptors();

            // Assert
            var action = Assert.Single(actions);
            Assert.Equal("stub/{controller}/{action}", action.AttributeRouteTemplate);
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

        [Route("api/Token/[key]/[controller]")]
        [MyRouteConstraint(false)]
        private class TokenReplacementController
        {
            [HttpGet("stub/[action]")]
            public void ThisIsAnAction() { }
        }

        private class CaseInsensitiveController
        {
            [HttpGet("stub/[ActIon]")]
            public void ThisIsAnAction() { }
        }

        private class MultipleErrorsController
        {
            [HttpGet("stub/[action]/[unknown]")]
            public void Unknown() { }

            [HttpGet("[invalid/syntax")]
            public void Invalid() { }
        }

        private class InvalidParametersController
        {
            [HttpGet("stub/{controller}/{action}")]
            public void Action1() { }
        }

        private class SameGroupIdController
        {
            [HttpGet("stub/[action]")]
            public void Action1() { }

            [HttpGet("stub/Action1")]
            public void Action2() { }
        }
    }
}
