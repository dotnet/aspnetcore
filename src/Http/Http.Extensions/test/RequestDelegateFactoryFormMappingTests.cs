// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;

namespace Microsoft.AspNetCore.Http;

public class RequestDelegateFactoryFormMappingTests
{
    [Fact]
    public void CreatePreservesBehaviorForFormClassWithoutPublicConstructors()
    {
        static void TestAction([FromForm] FormClassWithoutPublicConstructors value) { }

        var result = RequestDelegateFactory.Create(TestAction);

        Assert.NotNull(result.RequestDelegate);
    }

    [Fact]
    public void CreatePreservesConverterFailureForFormCollectionWithUnsupportedElementType()
    {
        static void TestAction([FromForm] List<FormClassWithoutPublicConstructors> value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal($"No converter registered for type '{typeof(List<FormClassWithoutPublicConstructors>).FullName}'.", exception.Message);
    }

    [Fact]
    public void CreatePreservesConverterFailureForFormDictionaryWithUnsupportedValueType()
    {
        static void TestAction([FromForm] Dictionary<string, FormClassWithoutPublicConstructors> value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal($"No converter registered for type '{typeof(FormClassWithoutPublicConstructors).FullName}'.", exception.Message);
    }

    [Fact]
    public void CreateThrowsForEnumerableFormClassWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] EnumerableFormClassWithMultipleConstructors value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal(
            "The form parameter 'value' has type 'EnumerableFormClassWithMultipleConstructors', which has multiple public constructors. Only a single public constructor is supported.",
            exception.Message);
    }

    [Fact]
    public void CreateThrowsForFormClassWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] FormClassWithMultipleConstructors value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal(
            "The form parameter 'value' has type 'FormClassWithMultipleConstructors', which has multiple public constructors. Only a single public constructor is supported.",
            exception.Message);
    }

    [Fact]
    public void CreateThrowsForFormStructWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] FormStructWithMultipleConstructors value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal(
            "The form parameter 'value' has type 'FormStructWithMultipleConstructors', which has multiple public constructors. Only a single public constructor is supported.",
            exception.Message);
    }

    private sealed class FormClassWithMultipleConstructors
    {
        public FormClassWithMultipleConstructors(int value)
        {
        }

        public FormClassWithMultipleConstructors(string value)
        {
        }
    }

    private sealed class FormClassWithoutPublicConstructors
    {
        private FormClassWithoutPublicConstructors()
        {
        }
    }

    private sealed class EnumerableFormClassWithMultipleConstructors : IEnumerable<int>
    {
        public EnumerableFormClassWithMultipleConstructors(int value)
        {
        }

        public EnumerableFormClassWithMultipleConstructors(string value)
        {
        }

        public IEnumerator<int> GetEnumerator() => Enumerable.Empty<int>().GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private struct FormStructWithMultipleConstructors
    {
        public FormStructWithMultipleConstructors(int value)
        {
        }

        public FormStructWithMultipleConstructors(string value)
        {
        }
    }
}
