// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Endpoints.FormMapping.Metadata;

public class FormDataMetadataFactoryTests
{
    [Fact]
    public void CanCreateMetadata_ForBasicClassTypes()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(Customer), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(Customer), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Id", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(int), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Name", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            });
    }

    [Fact]
    public void CanCreateMetadata_ForMoreComplexClassTypes()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(CustomerWithAddress), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(CustomerWithAddress), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.False(metadata.IsRecursive);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("BillingAddress", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(Address), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Object, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.False(property.PropertyMetadata.IsRecursive);
                Assert.Collection(property.PropertyMetadata.Properties,
                   property =>
                   {
                       Assert.Equal("Street", property.Property.Name);
                       Assert.NotNull(property.PropertyMetadata);
                       Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                       Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                       Assert.Null(property.PropertyMetadata.Constructor);
                       Assert.Empty(property.PropertyMetadata.Properties);
                   },
                   property =>
                   {
                       Assert.Equal("City", property.Property.Name);
                       Assert.NotNull(property.PropertyMetadata);
                       Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                       Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                       Assert.Null(property.PropertyMetadata.Constructor);
                       Assert.Empty(property.PropertyMetadata.Properties);
                   });
            },
            property =>
            {
                Assert.Equal("ShippingAddress", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(Address), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Object, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.False(property.PropertyMetadata.IsRecursive);
                Assert.Collection(property.PropertyMetadata.Properties,
                    property =>
                    {
                        Assert.Equal("Street", property.Property.Name);
                        Assert.NotNull(property.PropertyMetadata);
                        Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                        Assert.Null(property.PropertyMetadata.Constructor);
                        Assert.Empty(property.PropertyMetadata.Properties);
                    },
                    property =>
                    {
                        Assert.Equal("City", property.Property.Name);
                        Assert.NotNull(property.PropertyMetadata);
                        Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                        Assert.Null(property.PropertyMetadata.Constructor);
                        Assert.Empty(property.PropertyMetadata.Properties);
                    });
            });
    }

    [Fact]
    public void CanCreateMetadata_ForValueTypes()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(Address), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(Address), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.Null(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Street", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("City", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            });
    }

    [Fact]
    public void CanCreateMetadata_DetectsConstructors()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(KeyValuePair<int, string>), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(KeyValuePair<int, string>), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.ConstructorParameters,
            parameter =>
            {
                Assert.Equal("key", parameter.Name);
                Assert.NotNull(parameter.ParameterMetadata);
                Assert.Equal(typeof(int), parameter.ParameterMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, parameter.ParameterMetadata.Kind);
                Assert.Null(parameter.ParameterMetadata.Constructor);
                Assert.Empty(parameter.ParameterMetadata.Properties);
            },
            parameter =>
            {
                Assert.Equal("value", parameter.Name);
                Assert.NotNull(parameter.ParameterMetadata);
                Assert.Equal(typeof(string), parameter.ParameterMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, parameter.ParameterMetadata.Kind);
                Assert.Null(parameter.ParameterMetadata.Constructor);
                Assert.Empty(parameter.ParameterMetadata.Properties);
            });
    }

    [Fact]
    public void CanCreateMetadata_ForTypeWithCollection()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(CustomerWithOrders), options);
        Assert.NotNull(metadata);

        // Assert
        Assert.Equal(typeof(CustomerWithOrders), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Id", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(int), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Name", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Orders", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(List<CustomerWithOrders.Order>), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Collection, property.PropertyMetadata.Kind);
                Assert.NotNull(property.PropertyMetadata.ElementType);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Collection(
                    property.PropertyMetadata.ElementType.Properties,
                    subProperty =>
                    {
                        Assert.Equal("Id", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(int), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, subProperty.PropertyMetadata.Kind);
                        Assert.Null(subProperty.PropertyMetadata.Constructor);
                        Assert.Empty(subProperty.PropertyMetadata.Properties);
                    },
                    subProperty =>
                    {
                        Assert.Equal("ProductName", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(string), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, subProperty.PropertyMetadata.Kind);
                        Assert.Null(subProperty.PropertyMetadata.Constructor);
                        Assert.Empty(subProperty.PropertyMetadata.Properties);
                    });
            });
    }

    [Fact]
    public void CanCreateMetadata_ForTypeWithDictionary()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(CompanyWithWarehousesByLocation), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(CompanyWithWarehousesByLocation), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Name", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Warehouses", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(Dictionary<string, Warehouse>), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Dictionary, property.PropertyMetadata.Kind);
                Assert.NotNull(property.PropertyMetadata.KeyType);
                Assert.NotNull(property.PropertyMetadata.ValueType);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Collection(
                    property.PropertyMetadata.ValueType.Properties,
                    subProperty =>
                    {
                        Assert.Equal("Name", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(string), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, subProperty.PropertyMetadata.Kind);
                        Assert.Null(subProperty.PropertyMetadata.Constructor);
                        Assert.Empty(subProperty.PropertyMetadata.Properties);
                    },
                    subProperty =>
                    {
                        Assert.Equal("Address", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(Address), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Object, subProperty.PropertyMetadata.Kind);
                        Assert.Null(subProperty.PropertyMetadata.Constructor);
                        Assert.Collection(
                            subProperty.PropertyMetadata.Properties,
                            subSubProperty =>
                            {
                                Assert.Equal("Street", subSubProperty.Property.Name);
                                Assert.NotNull(subSubProperty.PropertyMetadata);
                                Assert.Equal(typeof(string), subSubProperty.PropertyMetadata.Type);
                                Assert.Equal(FormDataTypeKind.Primitive, subSubProperty.PropertyMetadata.Kind);
                                Assert.Null(subSubProperty.PropertyMetadata.Constructor);
                                Assert.Empty(subSubProperty.PropertyMetadata.Properties);
                            },
                            subSubProperty =>
                            {
                                Assert.Equal("City", subSubProperty.Property.Name);
                                Assert.NotNull(subSubProperty.PropertyMetadata);
                                Assert.Equal(typeof(string), subSubProperty.PropertyMetadata.Type);
                                Assert.Equal(FormDataTypeKind.Primitive, subSubProperty.PropertyMetadata.Kind);
                                Assert.Null(subSubProperty.PropertyMetadata.Constructor);
                                Assert.Empty(subSubProperty.PropertyMetadata.Properties);
                            });
                    });
            });
    }

    [Fact]
    public void CanCreateMetadata_ForRecursiveTypes()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(RecursiveList<string>), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(RecursiveList<string>), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.True(metadata.IsRecursive);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Head", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Tail", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(RecursiveList<string>), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Object, property.PropertyMetadata.Kind);
                Assert.Same(metadata, property.PropertyMetadata);
            });
    }

    [Fact]
    public void CanCreateMetadata_ForRecursiveTypesWithInheritance()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(BaseList<string>), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(BaseList<string>), metadata.Type);
        Assert.True(metadata.IsRecursive);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Head", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Tail", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(DerivedList<string>), property.PropertyMetadata.Type);
                Assert.True(property.PropertyMetadata.IsRecursive);
                Assert.Equal(FormDataTypeKind.Object, property.PropertyMetadata.Kind);
                Assert.NotSame(metadata, property.PropertyMetadata);
                Assert.Collection(property.PropertyMetadata.Properties,
                    subProperty =>
                    {
                        Assert.Equal("Head", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(string), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Primitive, subProperty.PropertyMetadata.Kind);
                        Assert.Null(subProperty.PropertyMetadata.Constructor);
                        Assert.Empty(subProperty.PropertyMetadata.Properties);
                    },
                    subProperty =>
                    {
                        Assert.Equal("Tail", subProperty.Property.Name);
                        Assert.NotNull(subProperty.PropertyMetadata);
                        Assert.Equal(typeof(DerivedList<string>), subProperty.PropertyMetadata.Type);
                        Assert.Equal(FormDataTypeKind.Object, subProperty.PropertyMetadata.Kind);
                        Assert.Same(property.PropertyMetadata, subProperty.PropertyMetadata);
                    });
            });
    }

    [Fact]
    public void CanCreateMetadata_ForRecursiveTypesCollections()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(Tree<string>), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(Tree<string>), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.True(metadata.IsRecursive);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Value", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Children", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(List<Tree<string>>), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Collection, property.PropertyMetadata.Kind);
                Assert.False(property.PropertyMetadata.IsRecursive);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
                Assert.Same(metadata, property.PropertyMetadata.ElementType);
            });
    }

    [Fact]
    public void CanCreateMetadata_ForRecursiveTypesDictionaries()
    {
        // Arrange
        var (factory, options) = ResolveFactory();
        // Act
        var metadata = factory.GetOrCreateMetadataFor(typeof(DictionaryTree<string>), options);

        // Assert
        Assert.NotNull(metadata);
        Assert.Equal(typeof(DictionaryTree<string>), metadata.Type);
        Assert.Equal(FormDataTypeKind.Object, metadata.Kind);
        Assert.True(metadata.IsRecursive);
        Assert.NotNull(metadata.Constructor);
        Assert.Collection(metadata.Properties,
            property =>
            {
                Assert.Equal("Value", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(string), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Primitive, property.PropertyMetadata.Kind);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
            },
            property =>
            {
                Assert.Equal("Children", property.Property.Name);
                Assert.NotNull(property.PropertyMetadata);
                Assert.Equal(typeof(Dictionary<string, DictionaryTree<string>>), property.PropertyMetadata.Type);
                Assert.Equal(FormDataTypeKind.Dictionary, property.PropertyMetadata.Kind);
                Assert.False(property.PropertyMetadata.IsRecursive);
                Assert.Null(property.PropertyMetadata.Constructor);
                Assert.Empty(property.PropertyMetadata.Properties);
                Assert.Same(metadata, property.PropertyMetadata.ValueType);
            });
    }

    private (FormDataMetadataFactory, FormDataMapperOptions) ResolveFactory()
    {
        var options = new FormDataMapperOptions();
        var factory = options.Factories.OfType<ComplexTypeConverterFactory>().Single().MetadataFactory;
        return (factory, options);
    }
}

public class CustomerWithOrders
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Order> Orders { get; set; }

    public class Order
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
    }
}

public class CompanyWithWarehousesByLocation
{
    public string Name { get; set; }
    public Dictionary<string, Warehouse> Warehouses { get; set; }
}

public class Warehouse
{
    public string Name { get; set; }
    public Address Address { get; set; }
}

public class RecursiveList<T>
{
    public T Head { get; set; }

    public RecursiveList<T> Tail { get; set; }
}

public class BaseList<T>
{
    public T Head { get; set; }
    public DerivedList<T> Tail { get; set; }
}

public class DerivedList<T> : BaseList<T>
{
}

public class Tree<T>
{
    public T Value { get; set; }
    public List<Tree<T>> Children { get; set; }
}

public class DictionaryTree<T>
{
    public T Value { get; set; }
    public Dictionary<string, DictionaryTree<T>> Children { get; set; }
}

public struct Address
{
    public string Street { get; set; }
    public string City { get; set; }
}

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class CustomerWithAddress
{
    public Address BillingAddress { get; set; }
    public Address ShippingAddress { get; set; }
}
