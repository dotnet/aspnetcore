// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc.Test
{
    public class ReflectedActionDescriptorProviderTests
    {
        [Fact]
        public void GetDescriptors_GetsDescriptorsOnlyForValidActionsInBaseAndDerivedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            // "NewMethod" is a public method declared with keyword "new".
            Assert.Equal(new[] { "GetFromDerived", "NewMethod", "GetFromBase" }, actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_OverridenRedirect_FromControllerClass()
        {
            // Arrange & Act
            var actionNames = GetDescriptors(typeof(BaseController).GetTypeInfo()).Select(a => a.Name);

            // Assert
            Assert.DoesNotContain("Redirect", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_PrivateMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("PrivateMethod", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_Constructor_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("DerivedController", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_OperatorOverloadingMethod_FromOperatorOverloadingController()
        {
            // Arrange & Act
            var actionDescriptors = GetDescriptors(typeof(OperatorOverloadingController).GetTypeInfo());

            // Assert
            Assert.Empty(actionDescriptors);
        }

        [Fact]
        public void GetDescriptors_Ignores_GenericMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("GenericMethod", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_OverridenNonActionMethod_FromDerivedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("OverridenNonActionMethod", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_MethodsFromObjectClass_FromUserDefinedController()
        {
            // Arrange
            var methodsFromObjectClass = typeof(object).GetMethods().Select(m => m.Name);

            // Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.Empty(methodsFromObjectClass.Intersect(actionNames));
        }

        [Fact]
        public void GetDescriptors_Ignores_StaticMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("StaticMethod", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_ProtectedStaticMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("ProtectedStaticMethod", actionNames);
        }

        [Fact]
        public void GetDescriptors_Ignores_PrivateStaticMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.DoesNotContain("PrivateStaticMethod", actionNames);
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
            var provider = GetProvider(typeof(BaseController).GetTypeInfo(), new IFilter[]
            {
                filter,
            });

            // Act
            var model = provider.BuildModel();

            // Assert
            var filters = model.Filters;
            Assert.Same(filter, Assert.Single(filters));
        }

        private IEnumerable<string> GetActionNamesFromDerivedController()
        {
            return GetDescriptors(typeof(DerivedController).GetTypeInfo()).Select(a => a.Name).ToArray();
        }

        private ReflectedActionDescriptorProvider GetProvider(
            TypeInfo controllerTypeInfo, 
            IEnumerable<IFilter> filters = null)
        {
            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(new Assembly[] { controllerTypeInfo.Assembly });

            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfo);

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                filters,
                new MockMvcOptionsAccessor());

            return provider;
        }

        private IEnumerable<ActionDescriptor> GetDescriptors(params TypeInfo[] controllerTypeInfos)
        {
            var assemblyProvider = new Mock<IControllerAssemblyProvider>();
            assemblyProvider
                .SetupGet(ap => ap.CandidateAssemblies)
                .Returns(controllerTypeInfos.Select(cti => cti.Assembly).Distinct());

            var conventions = new StaticActionDiscoveryConventions(controllerTypeInfos);

            var provider = new ReflectedActionDescriptorProvider(
                assemblyProvider.Object,
                conventions,
                null,
                new MockMvcOptionsAccessor());
            
            return provider.GetDescriptors();
        }

        private class DerivedController : BaseController
        {
            public void GetFromDerived() // Valid action method.
            {
            }

            [HttpGet]
            public override void OverridenNonActionMethod()
            {
            }

            public new void NewMethod() // Valid action method.
            {
            }

            public void GenericMethod<T>()
            {
            }

            private void PrivateMethod()
            {
            }

            public static void StaticMethod()
            {
            }

            protected static void ProtectedStaticMethod()
            {
            }

            private static void PrivateStaticMethod()
            {
            }
        }

        private class OperatorOverloadingController : Controller
        {
            public static OperatorOverloadingController operator +(
                OperatorOverloadingController c1,
                OperatorOverloadingController c2)
            {
                return new OperatorOverloadingController();
            }
        }

        private class BaseController : Controller
        {
            public void GetFromBase() // Valid action method.
            {
            }

            [NonAction]
            public virtual void OverridenNonActionMethod()
            {
            }

            [NonAction]
            public virtual void NewMethod()
            {
            }

            public override RedirectResult Redirect(string url)
            {
                return base.Redirect(url + "#RedirectOverride");
            }
        }

        [MyFilter(2)]
        private class FiltersController
        {
            [MyFilter(3)]
            public void FilterAction()
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

        private class HttpMethodController
        {
            [HttpPost]
            public void OnlyPost()
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

        private class MyRouteConstraintAttribute : RouteConstraintAttribute
        {
            public MyRouteConstraintAttribute(bool blockNonAttributedActions)
                : base("key", "value", blockNonAttributedActions)
            {
            }
        }
    }
}
