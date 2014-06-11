// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Moq;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class ReflectedActionDescriptorProviderTests
    {
        private DefaultActionDiscoveryConventions _actionDiscoveryConventions =
            new DefaultActionDiscoveryConventions();
        private IControllerDescriptorFactory _controllerDescriptorFactory = new DefaultControllerDescriptorFactory();
        private IParameterDescriptorFactory _parameterDescriptorFactory = new DefaultParameterDescriptorFactory();

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
            Assert.False(actionNames.Contains("Redirect"));
        }

        [Fact]
        public void GetDescriptors_Ignores_PrivateMethod_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.False(actionNames.Contains("PrivateMethod"));
        }

        [Fact]
        public void GetDescriptors_Ignores_Constructor_FromUserDefinedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.False(actionNames.Contains("DerivedController"));
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
            Assert.False(actionNames.Contains("GenericMethod"));
        }

        [Fact]
        public void GetDescriptors_Ignores_OverridenNonActionMethod_FromDerivedController()
        {
            // Arrange & Act
            var actionNames = GetActionNamesFromDerivedController();

            // Assert
            Assert.False(actionNames.Contains("OverridenNonActionMethod"));
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

        private IEnumerable<string> GetActionNamesFromDerivedController()
        {
            return GetDescriptors(typeof(DerivedController).GetTypeInfo()).Select(a => a.Name);
        }

        private IEnumerable<ActionDescriptor> GetDescriptors(TypeInfo controllerTypeInfo)
        {
            var provider = new ReflectedActionDescriptorProvider(null,
                _actionDiscoveryConventions,
                _controllerDescriptorFactory,
                _parameterDescriptorFactory,
                null);
            var testControllers = new TypeInfo[]
            {
                controllerTypeInfo,
            };
            var controllerDescriptors = testControllers
                .Select(t => _controllerDescriptorFactory.CreateControllerDescriptor(t));
            return provider.GetDescriptors(controllerDescriptors);
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
    }
}
