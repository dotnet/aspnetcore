// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Formatters.Xml;

public class EnumerableWrapperProviderFactoryTest
{
    public static TheoryData<Type, object, Type> EnumerableOfTInterfaceData
    {
        get
        {
            var serializableError = new SerializableError();
            serializableError.Add("key1", "key1-error");

            return new TheoryData<Type, object, Type>
                {
                    {
                        typeof(IEnumerable<string>),
                        new [] { "value1", "value2" },
                        typeof(DelegatingEnumerable<string, string>)
                    },
                    {
                        typeof(IEnumerable<int>),
                        new [] { 10, 20 },
                        typeof(DelegatingEnumerable<int, int>)
                    },
                    {
                        typeof(IEnumerable<Person>),
                        new [] { new Person() { Id =10, Name = "John" } },
                        typeof(DelegatingEnumerable<Person, Person>)
                    },
                    {
                        typeof(IEnumerable<SerializableError>),
                        new [] { serializableError },
                        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>)
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(EnumerableOfTInterfaceData))]
    public void Creates_WrapperProvider_EnumerableOfTInterface(
                                                                Type declaredType,
                                                                object objectToBeWrapped,
                                                                Type expectedWrappingType)
    {
        // Arrange
        var wrapperProviderFactories = GetWrapperProviderFactories();
        var enumerableWrapperProviderFactory = new EnumerableWrapperProviderFactory(wrapperProviderFactories);
        var wrapperProviderContext = new WrapperProviderContext(declaredType, isSerialization: true);

        // Act
        var wrapperProvider = enumerableWrapperProviderFactory.GetProvider(wrapperProviderContext);

        // Assert
        Assert.NotNull(wrapperProvider);
        Assert.Equal(expectedWrappingType, wrapperProvider.WrappingType);
    }

    public static TheoryData<Type, object, Type> QueryableOfTInterfaceData
    {
        get
        {
            var serializableError = new SerializableError();
            serializableError.Add("key1", "key1-error");

            return new TheoryData<Type, object, Type>
                {
                    {
                        typeof(IEnumerable<string>),
                        (new [] { "value1", "value2" }).AsQueryable(),
                        typeof(DelegatingEnumerable<string, string>)
                    },
                    {
                        typeof(IEnumerable<int>),
                        (new [] { 10, 20 }).AsQueryable(),
                        typeof(DelegatingEnumerable<int, int>)
                    },
                    {
                        typeof(IEnumerable<Person>),
                        (new [] { new Person() { Id =10, Name = "John" } }).AsQueryable(),
                        typeof(DelegatingEnumerable<Person, Person>)
                    },
                    {
                        typeof(IEnumerable<SerializableError>),
                        (new [] { serializableError }).AsQueryable(),
                        typeof(DelegatingEnumerable<SerializableErrorWrapper, SerializableError>)
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(QueryableOfTInterfaceData))]
    public void Creates_WrapperProvider_QueryableOfTInterface(
                                                                Type declaredType,
                                                                object objectToBeWrapped,
                                                                Type expectedWrappingType)
    {
        // Arrange
        var wrapperProviderFactories = GetWrapperProviderFactories();
        var enumerableWrapperProviderFactory = new EnumerableWrapperProviderFactory(wrapperProviderFactories);
        var wrapperProviderContext = new WrapperProviderContext(declaredType, isSerialization: true);

        // Act
        var wrapperProvider = enumerableWrapperProviderFactory.GetProvider(wrapperProviderContext);

        // Assert
        Assert.NotNull(wrapperProvider);
        Assert.Equal(expectedWrappingType, wrapperProvider.WrappingType);
    }

    public static TheoryData<Type, object> ConcreteEnumerableOfTData
    {
        get
        {
            var serializableError = new SerializableError();
            serializableError.Add("key1", "key1-error");

            return new TheoryData<Type, object>
                {
                    {
                        typeof(string), // 'string' implements IEnumerable<char>
                        "value"
                    },
                    {
                        typeof(List<int>),
                        (new [] { 10, 20 }).ToList()
                    },
                    {
                        typeof(List<Person>),
                        (new [] { new Person() { Id =10, Name = "John" } }).ToList()
                    },
                    {
                        typeof(List<SerializableError>),
                        (new [] { serializableError }).ToList()
                    },
                    {
                        typeof(PersonList),
                        new PersonList()
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(ConcreteEnumerableOfTData))]
    public void DoesNot_CreateWrapperProvider_ForConcrete_EnumerableOfTImplementations(
                                                                Type declaredType,
                                                                object objectToBeWrapped)
    {
        // Arrange
        var wrapperProviderFactories = GetWrapperProviderFactories();
        var enumerableWrapperProviderFactory = new EnumerableWrapperProviderFactory(wrapperProviderFactories);
        var wrapperProviderContext = new WrapperProviderContext(declaredType, isSerialization: true);

        // Act
        var wrapperProvider = enumerableWrapperProviderFactory.GetProvider(wrapperProviderContext);

        // Assert
        Assert.Null(wrapperProvider);
    }

    private IEnumerable<IWrapperProviderFactory> GetWrapperProviderFactories()
    {
        var wrapperProviderFactories = new List<IWrapperProviderFactory>();
        wrapperProviderFactories.Add(new EnumerableWrapperProviderFactory(wrapperProviderFactories));
        wrapperProviderFactories.Add(new SerializableErrorWrapperProviderFactory());

        return wrapperProviderFactories;
    }

    internal class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    internal class PersonList : List<Person>
    {
    }
}
