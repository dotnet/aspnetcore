// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNet.Mvc
{
    public class PropertyHelperTest
    {
        [Fact]
        public void PropertyHelper_ReturnsNameCorrectly()
        {
            // Arrange
            var anonymous = new { foo = "bar" };
            PropertyInfo property = anonymous.GetType().GetProperties().First();

            // Act
            PropertyHelper helper = new PropertyHelper(property);

            // Assert
            Assert.Equal("foo", property.Name);
            Assert.Equal("foo", helper.Name);
        }

        [Fact]
        public void PropertyHelper_ReturnsValueCorrectly()
        {
            // Arrange
            var anonymous = new { bar = "baz" };
            PropertyInfo property = anonymous.GetType().GetProperties().First();

            // Act
            PropertyHelper helper = new PropertyHelper(property);

            // Assert
            Assert.Equal("bar", helper.Name);
            Assert.Equal("baz", helper.GetValue(anonymous));
        }

        [Fact]
        public void PropertyHelper_ReturnsValueCorrectly_ForValueTypes()
        {
            // Arrange
            var anonymous = new { foo = 32 };
            var property = anonymous.GetType().GetProperties().First();

            // Act
            var helper = new PropertyHelper(property);

            // Assert
            Assert.Equal("foo", helper.Name);
            Assert.Equal(32, helper.GetValue(anonymous));
        }

        [Fact]
        public void PropertyHelper_ReturnsCachedPropertyHelper()
        {
            // Arrange
            var anonymous = new { foo = "bar" };

            // Act
            var helpers1 = PropertyHelper.GetProperties(anonymous);
            var helpers2 = PropertyHelper.GetProperties(anonymous);

            // Assert
            Assert.Equal(1, helpers1.Length);
            Assert.Same(helpers1, helpers2);
            Assert.Same(helpers1[0], helpers2[0]);
        }

        [Fact]
        public void PropertyHelper_DoesNotChangeUnderscores()
        {
            // Arrange
            var anonymous = new { bar_baz2 = "foo" };

            // Act + Assert
            var helper = Assert.Single(PropertyHelper.GetProperties(anonymous));
            Assert.Equal("bar_baz2", helper.Name);
        }

        [Fact]
        public void PropertyHelper_DoesNotFindPrivateProperties()
        {
            // Arrange
            var anonymous = new PrivateProperties();

            // Act + Assert
            var helper = Assert.Single(PropertyHelper.GetProperties(anonymous));
            Assert.Equal("Prop1", helper.Name);
        }

        [Fact]
        public void PropertyHelper_DoesNotFindStaticProperties()
        {
            // Arrange
            var anonymous = new Static();

            // Act + Assert
            var helper = Assert.Single(PropertyHelper.GetProperties(anonymous));
            Assert.Equal("Prop5", helper.Name);
        }

        [Fact]
        public void PropertyHelper_DoesNotFindSetOnlyProperties()
        {
            // Arrange
            var anonymous = new SetOnly();

            // Act + Assert
            var helper = Assert.Single(PropertyHelper.GetProperties(anonymous));
            Assert.Equal("Prop6", helper.Name);
        }

        [Fact]
        public void PropertyHelper_WorksForStruct()
        {
            // Arrange
            var anonymous = new MyProperties();

            anonymous.IntProp = 3;
            anonymous.StringProp = "Five";

            // Act + Assert
            var helper1 = Assert.Single(PropertyHelper.GetProperties(anonymous).Where(prop => prop.Name == "IntProp"));
            var helper2 = Assert.Single(PropertyHelper.GetProperties(anonymous).Where(prop => prop.Name == "StringProp"));
            Assert.Equal(3, helper1.GetValue(anonymous));
            Assert.Equal("Five", helper2.GetValue(anonymous));
        }

        [Fact]
        public void PropertyHelper_ForDerivedClass()
        {
            // Arrange
            var derived = new DerivedClass { PropA = "propAValue", PropB = "propBValue" };

            // Act
            var helpers = PropertyHelper.GetProperties(derived).ToArray();

            // Assert
            Assert.NotNull(helpers);
            Assert.Equal(2, helpers.Length);

            var propAHelper = Assert.Single(helpers.Where(h => h.Name == "PropA"));
            var propBHelper = Assert.Single(helpers.Where(h => h.Name == "PropB"));

            Assert.Equal("propAValue", propAHelper.GetValue(derived));
            Assert.Equal("propBValue", propBHelper.GetValue(derived));
        }

        [Fact]
        public void PropertyHelper_ForDerivedClass_WithNew()
        {
            // Arrange
            var derived = new DerivedClassWithNew { PropA = "propAValue" };

            // Act
            var helpers = PropertyHelper.GetProperties(derived).ToArray();

            // Assert
            Assert.NotNull(helpers);
            Assert.Equal(2, helpers.Length);

            var propAHelper = Assert.Single(helpers.Where(h => h.Name == "PropA"));
            var propBHelper = Assert.Single(helpers.Where(h => h.Name == "PropB"));

            Assert.Equal("propAValue", propAHelper.GetValue(derived));
            Assert.Equal("Newed", propBHelper.GetValue(derived));
        }

        [Fact]
        public void PropertyHelper_ForDerived_WithVirtual()
        {
            // Arrange
            var derived = new DerivedClassWithOverride { PropA = "propAValue", PropB = "propBValue" };

            // Act
            var helpers = PropertyHelper.GetProperties(derived).ToArray();

            // Assert
            Assert.NotNull(helpers);
            Assert.Equal(2, helpers.Length);

            var propAHelper = Assert.Single(helpers.Where(h => h.Name == "PropA"));
            var propBHelper = Assert.Single(helpers.Where(h => h.Name == "PropB"));

            Assert.Equal("OverridenpropAValue", propAHelper.GetValue(derived));
            Assert.Equal("propBValue", propBHelper.GetValue(derived));
        }

        [Fact]
        public void MakeFastPropertySetter_SetsPropertyValues_ForPublicAndNobPublicProperties()
        {
            // Arrange
            var instance = new BaseClass();
            var typeInfo = instance.GetType().GetTypeInfo();
            var publicProperty = typeInfo.GetDeclaredProperty("PropA");
            var protectedProperty = typeInfo.GetDeclaredProperty("PropProtected");
            var publicPropertySetter = PropertyHelper.MakeFastPropertySetter(publicProperty);
            var protectedPropertySetter = PropertyHelper.MakeFastPropertySetter(protectedProperty);

            // Act
            publicPropertySetter(instance, "TestPublic");
            protectedPropertySetter(instance, "TestProtected");

            // Assert
            Assert.Equal("TestPublic", instance.PropA);
            Assert.Equal("TestProtected", instance.GetPropProtected());
        }

        [Fact]
        public void MakeFastPropertySetter_SetsPropertyValues_ForOverridenProperties()
        {
            // Arrange
            var instance = new DerivedClassWithOverride();
            var typeInfo = instance.GetType().GetTypeInfo();
            var property = typeInfo.GetDeclaredProperty("PropA");
            var propertySetter = PropertyHelper.MakeFastPropertySetter(property);

            // Act
            propertySetter(instance, "Test value");

            // Assert
            Assert.Equal("OverridenTest value", instance.PropA);
        }

        [Fact]
        public void MakeFastPropertySetter_SetsPropertyValues_ForNewedProperties()
        {
            // Arrange
            var instance = new DerivedClassWithNew();
            var typeInfo = instance.GetType().GetTypeInfo();
            var property = typeInfo.GetDeclaredProperty("PropB");
            var propertySetter = PropertyHelper.MakeFastPropertySetter(property);

            // Act
            propertySetter(instance, "Test value");

            // Assert
            Assert.Equal("NewedTest value", instance.PropB);
        }

        private class Static
        {
            public static int Prop2 { get; set; }
            public int Prop5 { get; set; }
        }

        private struct MyProperties
        {
            public int IntProp { get; set; }
            public string StringProp { get; set; }
        }

        private class SetOnly
        {
            public int Prop2 { set { } }
            public int Prop6 { get; set; }
        }

        private class PrivateProperties
        {
            public int Prop1 { get; set; }
            protected int Prop2 { get; set; }
            private int Prop3 { get; set; }
        }

        public class BaseClass
        {
            public string PropA { get; set; }

            protected string PropProtected { get; set; }

            public string GetPropProtected()
            {
                return PropProtected;
            }
        }

        public class DerivedClass : BaseClass
        {
            public string PropB { get; set; }
        }

        public class BaseClassWithVirtual
        {
            public virtual string PropA { get; set; }
            public string PropB { get; set; }
        }

        public class DerivedClassWithNew : BaseClassWithVirtual
        {
            private string _value = "Newed";

            public new string PropB
            {
                get { return _value; }
                set { _value = "Newed" + value; }
            }
        }

        public class DerivedClassWithOverride : BaseClassWithVirtual
        {
            private string _value = "Overriden";

            public override string PropA
            {
                get { return _value; }
                set { _value = "Overriden" + value; }
            }
        }
    }
}
