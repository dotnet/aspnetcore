// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class PropertyHelperTest
{
    [Fact]
    public void PropertyHelper_ReturnsNameCorrectly()
    {
        // Arrange
        var anonymous = new { foo = "bar" };
        var property = PropertyHelper.GetProperties(anonymous.GetType()).First().Property;

        // Act
        var helper = new PropertyHelper(property);

        // Assert
        Assert.Equal("foo", property.Name);
        Assert.Equal("foo", helper.Name);
    }

    [Fact]
    public void PropertyHelper_ReturnsValueCorrectly()
    {
        // Arrange
        var anonymous = new { bar = "baz" };
        var property = PropertyHelper.GetProperties(anonymous.GetType()).First().Property;

        // Act
        var helper = new PropertyHelper(property);

        // Assert
        Assert.Equal("bar", helper.Name);
        Assert.Equal("baz", helper.GetValue(anonymous));
    }

    [Fact]
    public void PropertyHelper_ReturnsGetterDelegate()
    {
        // Arrange
        var anonymous = new { bar = "baz" };
        var property = PropertyHelper.GetProperties(anonymous.GetType()).First().Property;

        // Act
        var helper = new PropertyHelper(property);

        // Assert
        Assert.NotNull(helper.ValueGetter);
        Assert.Equal("baz", helper.ValueGetter(anonymous));
    }

    [Fact]
    public void SetValue_SetsPropertyValue()
    {
        // Arrange
        var expected = "new value";
        var instance = new BaseClass { PropA = "old value" };
        var helper = PropertyHelper.GetProperties(
            instance.GetType()).First(prop => prop.Name == "PropA");

        // Act
        helper.SetValue(instance, expected);

        // Assert
        Assert.Equal(expected, instance.PropA);
    }

    [Fact]
    public void PropertyHelper_ReturnsSetterDelegate()
    {
        // Arrange
        var expected = "new value";
        var instance = new BaseClass { PropA = "old value" };
        var helper = PropertyHelper.GetProperties(
            instance.GetType()).First(prop => prop.Name == "PropA");

        // Act and Assert
        Assert.NotNull(helper.ValueSetter);
        helper.ValueSetter(instance, expected);

        // Assert
        Assert.Equal(expected, instance.PropA);
    }

    [Fact]
    public void PropertyHelper_ReturnsValueCorrectly_ForValueTypes()
    {
        // Arrange
        var anonymous = new { foo = 32 };
        var property = PropertyHelper.GetProperties(anonymous.GetType()).First().Property;

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
        var helpers1 = PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo());
        var helpers2 = PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo());

        // Assert
        Assert.Single(helpers1);
        Assert.Same(helpers1, helpers2);
        Assert.Same(helpers1[0], helpers2[0]);
    }

    [Fact]
    public void PropertyHelper_DoesNotChangeUnderscores()
    {
        // Arrange
        var anonymous = new { bar_baz2 = "foo" };

        // Act + Assert
        var helper = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()));
        Assert.Equal("bar_baz2", helper.Name);
    }

    [Fact]
    public void PropertyHelper_DoesNotFindPrivateProperties()
    {
        // Arrange
        var anonymous = new PrivateProperties();

        // Act + Assert
        var helper = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()));
        Assert.Equal("Prop1", helper.Name);
    }

    [Fact]
    public void PropertyHelper_DoesNotFindStaticProperties()
    {
        // Arrange
        var anonymous = new Static();

        // Act + Assert
        var helper = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()));
        Assert.Equal("Prop5", helper.Name);
    }

#if NETSTANDARD || NETCOREAPP
    [Fact]
    public void PropertyHelper_RefStructProperties()
    {
        // Arrange
        var obj = new RefStructProperties();

        // Act + Assert
        var helper = Assert.Single(PropertyHelper.GetProperties(obj.GetType().GetTypeInfo()));
        Assert.Equal("Prop5", helper.Name);
    }
#elif NETFRAMEWORK
#else
#error Unknown TFM - update the set of TFMs where we test for ref structs
#endif

    [Fact]
    public void PropertyHelper_DoesNotFindSetOnlyProperties()
    {
        // Arrange
        var anonymous = new SetOnly();

        // Act + Assert
        var helper = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()));
        Assert.Equal("Prop6", helper.Name);
    }

    [Theory]
    [InlineData(typeof(int?))]
    [InlineData(typeof(DayOfWeek?))]
    public void PropertyHelper_WorksForNullablePrimitiveAndEnumTypes(Type nullableType)
    {
        // Act
        var properties = PropertyHelper.GetProperties(nullableType);

        // Assert
        Assert.Empty(properties);
    }

    [Fact]
    public void PropertyHelper_UnwrapsNullableTypes()
    {
        // Arrange
        var myType = typeof(MyStruct?);

        // Act
        var properties = PropertyHelper.GetProperties(myType);

        // Assert
        var property = Assert.Single(properties);
        Assert.Equal("Foo", property.Name);
    }

    [Fact]
    public void PropertyHelper_WorksForStruct()
    {
        // Arrange
        var anonymous = new MyProperties();

        anonymous.IntProp = 3;
        anonymous.StringProp = "Five";

        // Act + Assert
        var helper1 = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()).Where(prop => prop.Name == "IntProp"));
        var helper2 = Assert.Single(PropertyHelper.GetProperties(anonymous.GetType().GetTypeInfo()).Where(prop => prop.Name == "StringProp"));
        Assert.Equal(3, helper1.GetValue(anonymous));
        Assert.Equal("Five", helper2.GetValue(anonymous));
    }

    [Fact]
    public void PropertyHelper_ForDerivedClass()
    {
        // Arrange
        var derived = new DerivedClass { PropA = "propAValue", PropB = "propBValue" };

        // Act
        var helpers = PropertyHelper.GetProperties(derived.GetType().GetTypeInfo()).ToArray();

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
        var helpers = PropertyHelper.GetProperties(derived.GetType().GetTypeInfo()).ToArray();

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
        var helpers = PropertyHelper.GetProperties(derived.GetType().GetTypeInfo()).ToArray();

        // Assert
        Assert.NotNull(helpers);
        Assert.Equal(2, helpers.Length);

        var propAHelper = Assert.Single(helpers.Where(h => h.Name == "PropA"));
        var propBHelper = Assert.Single(helpers.Where(h => h.Name == "PropB"));

        Assert.Equal("OverridenpropAValue", propAHelper.GetValue(derived));
        Assert.Equal("propBValue", propBHelper.GetValue(derived));
    }

    [Fact]
    public void PropertyHelper_ForInterface_ReturnsExpectedProperties()
    {
        // Arrange
        var expectedNames = new[] { "Count", "IsReadOnly" };

        // Act
        var helpers = PropertyHelper.GetProperties(typeof(ICollection<string>));

        // Assert
        Assert.Collection(
            helpers.OrderBy(helper => helper.Name, StringComparer.Ordinal),
            helper => { Assert.Equal(expectedNames[0], helper.Name, StringComparer.Ordinal); },
            helper => { Assert.Equal(expectedNames[1], helper.Name, StringComparer.Ordinal); });
    }

    [Fact]
    public void PropertyHelper_ForDerivedInterface_ReturnsAllProperties()
    {
        // Arrange
        var expectedNames = new[] { "Count", "IsReadOnly", "Keys", "Values" };

        // Act
        var helpers = PropertyHelper.GetProperties(typeof(IDictionary<string, string>));

        // Assert
        Assert.Collection(
            helpers.OrderBy(helper => helper.Name, StringComparer.Ordinal),
            helper => { Assert.Equal(expectedNames[0], helper.Name, StringComparer.Ordinal); },
            helper => { Assert.Equal(expectedNames[1], helper.Name, StringComparer.Ordinal); },
            helper => { Assert.Equal(expectedNames[2], helper.Name, StringComparer.Ordinal); },
            helper => { Assert.Equal(expectedNames[3], helper.Name, StringComparer.Ordinal); });
    }

    [Fact]
    public void GetProperties_ExcludesIndexersAndPropertiesWithoutPublicGetters()
    {
        // Arrange
        var type = typeof(DerivedClassWithNonReadableProperties);

        // Act
        var result = PropertyHelper.GetProperties(type).ToArray();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Equal("Visible", result[0].Name);
        Assert.Equal("PropA", result[1].Name);
        Assert.Equal("PropB", result[2].Name);
    }

    [Fact]
    public void GetVisibleProperties_NoHiddenProperty()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type).ToArray();

        // Assert
        var property = Assert.Single(result);
        Assert.Equal("Length", property.Name);
        Assert.Equal(typeof(int), property.Property.PropertyType);
    }

    [Fact]
    public void GetVisibleProperties_HiddenProperty()
    {
        // Arrange
        var type = typeof(DerivedHiddenProperty);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Id", result[0].Name);
        Assert.Equal(typeof(string), result[0].Property.PropertyType);
        Assert.Equal("Name", result[1].Name);
        Assert.Equal(typeof(string), result[1].Property.PropertyType);
    }

    [Fact]
    public void GetVisibleProperties_HiddenProperty_TwoLevels()
    {
        // Arrange
        var type = typeof(DerivedHiddenProperty2);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Id", result[0].Name);
        Assert.Equal(typeof(Guid), result[0].Property.PropertyType);
        Assert.Equal("Name", result[1].Name);
        Assert.Equal(typeof(string), result[1].Property.PropertyType);
    }

    [Fact]
    public void GetVisibleProperties_NoHiddenPropertyWithTypeInfoInput()
    {
        // Arrange
        var type = typeof(string);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type.GetTypeInfo()).ToArray();

        // Assert
        var property = Assert.Single(result);
        Assert.Equal("Length", property.Name);
        Assert.Equal(typeof(int), property.Property.PropertyType);
    }

    [Fact]
    public void GetVisibleProperties_HiddenPropertyWithTypeInfoInput()
    {
        // Arrange
        var type = typeof(DerivedHiddenProperty);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type.GetTypeInfo()).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Id", result[0].Name);
        Assert.Equal(typeof(string), result[0].Property.PropertyType);
        Assert.Equal("Name", result[1].Name);
        Assert.Equal(typeof(string), result[1].Property.PropertyType);
    }

    [Fact]
    public void GetVisibleProperties_HiddenProperty_TwoLevelsWithTypeInfoInput()
    {
        // Arrange
        var type = typeof(DerivedHiddenProperty2);

        // Act
        var result = PropertyHelper.GetVisibleProperties(type.GetTypeInfo()).ToArray();

        // Assert
        Assert.Equal(2, result.Length);
        Assert.Equal("Id", result[0].Name);
        Assert.Equal(typeof(Guid), result[0].Property.PropertyType);
        Assert.Equal("Name", result[1].Name);
        Assert.Equal(typeof(string), result[1].Property.PropertyType);
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

    [Fact]
    public void MakeFastPropertyGetter_ReferenceType_ForNullObject_Throws()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(BaseClass))
            .Single(p => p.Name == nameof(BaseClass.PropA));

        var accessor = PropertyHelper.MakeFastPropertyGetter(property.Property);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => accessor(null));
    }

    [Fact]
    public void MakeFastPropertyGetter_ValueType_ForNullObject_Throws()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(MyProperties))
            .Single(p => p.Name == nameof(MyProperties.StringProp));

        var accessor = PropertyHelper.MakeFastPropertyGetter(property.Property);

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => accessor(null));
    }

    [Fact]
    public void MakeNullSafeFastPropertyGetter_ReferenceType_Success()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(BaseClass))
            .Single(p => p.Name == nameof(BaseClass.PropA));

        var accessor = PropertyHelper.MakeNullSafeFastPropertyGetter(property.Property);

        // Act
        var value = accessor(new BaseClass() { PropA = "Hi" });

        // Assert
        Assert.Equal("Hi", value);
    }

    [Fact]
    public void MakeNullSafeFastPropertyGetter_ValueType_Success()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(MyProperties))
            .Single(p => p.Name == nameof(MyProperties.StringProp));

        var accessor = PropertyHelper.MakeNullSafeFastPropertyGetter(property.Property);

        // Act
        var value = accessor(new MyProperties() { StringProp = "Hi" });

        // Assert
        Assert.Equal("Hi", value);
    }

    [Fact]
    public void MakeNullSafeFastPropertyGetter_ReferenceType_ForNullObject_ReturnsNull()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(BaseClass))
            .Single(p => p.Name == nameof(BaseClass.PropA));

        var accessor = PropertyHelper.MakeNullSafeFastPropertyGetter(property.Property);

        // Act
        var value = accessor(null);

        // Assert
        Assert.Null(value);
    }

    [Fact]
    public void MakeNullSafeFastPropertyGetter_ValueType_ForNullObject_ReturnsNull()
    {
        // Arrange
        var property = PropertyHelper
            .GetProperties(typeof(MyProperties))
            .Single(p => p.Name == nameof(MyProperties.StringProp));

        var accessor = PropertyHelper.MakeNullSafeFastPropertyGetter(property.Property);

        // Act
        var value = accessor(null);

        // Assert
        Assert.Null(value);
    }

    public static TheoryData<object, KeyValuePair<string, object>> IgnoreCaseTestData
    {
        get
        {
            return new TheoryData<object, KeyValuePair<string, object>>
                {
                    {
                        new
                        {
                            selected = true,
                            SeLeCtEd = false
                        },
                        new KeyValuePair<string, object>("selected", false)
                    },
                    {
                        new
                        {
                            SeLeCtEd = false,
                            selected = true
                        },
                        new KeyValuePair<string, object>("SeLeCtEd", true)
                    },
                    {
                        new
                        {
                            SelECTeD = false,
                            SeLECTED = true
                        },
                        new KeyValuePair<string, object>("SelECTeD", true)
                    }
                };
        }
    }

    [Theory]
    [MemberData(nameof(IgnoreCaseTestData))]
    public void ObjectToDictionary_IgnoresPropertyCase(object testObject,
                                                       KeyValuePair<string, object> expectedEntry)
    {
        // Act
        var result = PropertyHelper.ObjectToDictionary(testObject);

        // Assert
        var entry = Assert.Single(result);
        Assert.Equal(expectedEntry, entry);
    }

    [Fact]
    public void ObjectToDictionary_WithNullObject_ReturnsEmptyDictionary()
    {
        // Arrange
        object value = null;

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(0, dictValues.Count);
    }

    [Fact]
    public void ObjectToDictionary_WithPlainObjectType_ReturnsEmptyDictionary()
    {
        // Arrange
        var value = new object();

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(0, dictValues.Count);
    }

    [Fact]
    public void ObjectToDictionary_WithPrimitiveType_LooksUpPublicProperties()
    {
        // Arrange
        var value = "test";

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(1, dictValues.Count);
        Assert.Equal(4, dictValues["Length"]);
    }

    [Fact]
    public void ObjectToDictionary_WithAnonymousType_LooksUpProperties()
    {
        // Arrange
        var value = new { test = "value", other = 1 };

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(2, dictValues.Count);
        Assert.Equal("value", dictValues["test"]);
        Assert.Equal(1, dictValues["other"]);
    }

    [Fact]
    public void ObjectToDictionary_ReturnsCaseInsensitiveDictionary()
    {
        // Arrange
        var value = new { TEST = "value", oThEr = 1 };

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(2, dictValues.Count);
        Assert.Equal("value", dictValues["test"]);
        Assert.Equal(1, dictValues["other"]);
    }

    [Fact]
    public void ObjectToDictionary_ReturnsInheritedProperties()
    {
        // Arrange
        var value = new ThreeDPoint() { X = 5, Y = 10, Z = 17 };

        // Act
        var dictValues = PropertyHelper.ObjectToDictionary(value);

        // Assert
        Assert.NotNull(dictValues);
        Assert.Equal(3, dictValues.Count);
        Assert.Equal(5, dictValues["X"]);
        Assert.Equal(10, dictValues["Y"]);
        Assert.Equal(17, dictValues["Z"]);
    }

    private class Point
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    private class ThreeDPoint : Point
    {
        public int Z { get; set; }
    }

    private class Static
    {
        public static int Prop2 { get; set; }
        public int Prop5 { get; set; }
    }

#if NETSTANDARD || NETCOREAPP
    private class RefStructProperties
    {
        public Span<bool> Span => throw new NotImplementedException();
        public MyRefStruct UserDefined => throw new NotImplementedException();

        public int Prop5 { get; set; }
    }

    private readonly ref struct MyRefStruct
    {
    }
#elif NETFRAMEWORK
#else
#error Unknown TFM - update the set of TFMs where we test for ref structs
#endif
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

    private class DerivedClassWithNonReadableProperties : BaseClassWithVirtual
    {
        public string this[int index]
        {
            get { return string.Empty; }
            set { }
        }

        public int Visible { get; set; }

        private string NotVisible { get; set; }

        public string NotVisible2 { private get; set; }

        public string NotVisible3
        {
            set { }
        }

        public static string NotVisible4 { get; set; }
    }

    private struct MyStruct
    {
        public string Foo { get; set; }
    }

    private class BaseHiddenProperty
    {
        public int Id { get; set; }
    }

    private class DerivedHiddenProperty : BaseHiddenProperty
    {
        public new string Id { get; set; }

        public string Name { get; set; }
    }

    private class DerivedHiddenProperty2 : DerivedHiddenProperty
    {
        public new Guid Id { get; set; }

        public new string Name { get; private set; }
    }
}
