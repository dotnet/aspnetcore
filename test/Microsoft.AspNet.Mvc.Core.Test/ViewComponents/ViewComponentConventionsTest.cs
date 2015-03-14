// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNet.Mvc.ViewComponentConventionsTestClasses;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class ViewComponentConventionsTest
    {
        [Theory]

        // Only public top-level classes can be view components.
        [InlineData(typeof(PublicClass), true)]
        [InlineData(typeof(InternalClass), false)]
        [InlineData(typeof(PublicNestedClass), false)]
        [InlineData(typeof(PrivateNestedClass), false)]

        // Abstract classes, interfaces, and open generics don't work either.
        [InlineData(typeof(AbstractClass), false)]
        [InlineData(typeof(IAmAnInterfaceViewComponent), false)]
        [InlineData(typeof(GenericViewComponent<>), false)]
        [InlineData(typeof(GenericViewComponent<string>), true)]

        // You need the attribute, or a naming convention
        [InlineData(typeof(Nada), false)]

        // Naming convention doesn't apply to derived classes that don't follow it.
        [InlineData(typeof(NamingConventionViewComponent), true)]
        [InlineData(typeof(CaseInsensitiveNamingConventionVIEWCOMPONENT), true)]
        [InlineData(typeof(DerivedNamingConvention), false)]

        // The Attribute does apply to derived classes.
        [InlineData(typeof(WithAttribute), true)]
        [InlineData(typeof(DerivedWithAttribute), true)]
        public void IsComponent(Type type, bool expected)
        {
            // Arrange & Act
            var result = ViewComponentConventions.IsComponent(type.GetTypeInfo());

            // Assert
            Assert.Equal(expected, result);
        }

        public class PublicNestedClass : ViewComponent
        {
        }

        private class PrivateNestedClass : ViewComponent
        {
        }
    }
}

// These types need to be public/non-nested for validity of the test
namespace Microsoft.AspNet.Mvc.ViewComponentConventionsTestClasses
{
    public class PublicClass : ViewComponent
    {
    }

    internal class InternalClass : ViewComponent
    {
    }

    public abstract class AbstractClass : ViewComponent
    {
    }

    public class GenericViewComponent<T> : ViewComponent
    {
    }

    public interface IAmAnInterfaceViewComponent
    {
    }

    public class Nada
    {
    }

    public class NamingConventionViewComponent
    {
    }

    public class DerivedNamingConvention : NamingConventionViewComponent
    {
    }

    public class CaseInsensitiveNamingConventionVIEWCOMPONENT
    {
    }


    [ViewComponent]
    public class WithAttribute
    {
    }

    public class DerivedWithAttribute : WithAttribute
    {
    }
}