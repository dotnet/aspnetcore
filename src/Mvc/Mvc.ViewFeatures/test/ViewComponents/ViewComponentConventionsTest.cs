// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses;

namespace Microsoft.AspNetCore.Mvc.ViewComponents
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
        [InlineData(typeof(WithAttributeAndName), true)]
        [InlineData(typeof(DerivedWithAttributeAndName), true)]
        [InlineData(typeof(DerivedWithOverriddenAttributeName), true)]

        // Value types cannot be view components
        [InlineData(typeof(int), false)]

        // If it has NonViewComponent it's not a view component
        [InlineData(typeof(NonViewComponentAttributeViewComponent), false)]
        [InlineData(typeof(ChildOfNonViewComponent), false)]
        public void IsComponent(Type type, bool expected)
        {
            // Arrange & Act
            var result = ViewComponentConventions.IsComponent(type.GetTypeInfo());

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(typeof(PublicClass), "PublicClass")]
        [InlineData(typeof(GenericViewComponent<string>), "GenericViewComponent`1")]
        [InlineData(typeof(NamingConventionViewComponent), "NamingConvention")]
        [InlineData(typeof(CaseInsensitiveNamingConventionVIEWCOMPONENT), "CaseInsensitiveNamingConvention")]
        [InlineData(typeof(WithAttribute), "WithAttribute")]
        [InlineData(typeof(DerivedWithAttribute), "DerivedWithAttribute")]
        [InlineData(typeof(WithAttributeAndName), "Name")]
        [InlineData(typeof(DerivedWithAttributeAndName), "Name")]
        [InlineData(typeof(DerivedWithOverriddenAttributeName), "Name")]
        public void GetComponentName(Type type, string expected)
        {
            // Arrange & Act
            var result = ViewComponentConventions.GetComponentName(type.GetTypeInfo());

            // Assert
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(typeof(PublicClass), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.PublicClass")]
        [InlineData(typeof(GenericViewComponent<string>), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.GenericViewComponent`1")]
        [InlineData(typeof(NamingConventionViewComponent), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.NamingConvention")]
        [InlineData(typeof(CaseInsensitiveNamingConventionVIEWCOMPONENT), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.CaseInsensitiveNamingConvention")]
        [InlineData(typeof(WithAttribute), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.WithAttribute")]
        [InlineData(typeof(DerivedWithAttribute), "Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses.DerivedWithAttribute")]
        [InlineData(typeof(WithAttributeAndName), "Name")]
        [InlineData(typeof(DerivedWithAttributeAndName), "Name")]
        [InlineData(typeof(DerivedWithOverriddenAttributeName), "New.Name")]
        public void GetComponentFullName(Type type, string expected)
        {
            // Arrange & Act
            var result = ViewComponentConventions.GetComponentFullName(type.GetTypeInfo());

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
namespace Microsoft.AspNetCore.Mvc.ViewComponentConventionsTestClasses
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

    [NonViewComponent]
    public class NonViewComponentAttributeViewComponent
    { }

    public class ChildOfNonViewComponent : NonViewComponentAttributeViewComponent
    { }

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

    [ViewComponent(Name = "Name")]
    public class WithAttributeAndName
    {
    }

    public class DerivedWithAttributeAndName : WithAttributeAndName
    {
    }

    [ViewComponent(Name = "New.Name")]
    public class DerivedWithOverriddenAttributeName : WithAttributeAndName
    {
    }
}
