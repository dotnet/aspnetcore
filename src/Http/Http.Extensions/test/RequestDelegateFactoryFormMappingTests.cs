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
    public void CreateSupportsFormCollectionWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] List<int> value) { }

        var result = RequestDelegateFactory.Create(TestAction);

        Assert.NotNull(result.RequestDelegate);
    }

    [Fact]
    public void CreateSupportsFormDictionaryWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] Dictionary<string, int> value) { }

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
    public void CreatePreservesConverterFailureForFormClassWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] FormClassWithMultipleConstructors value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal($"No converter registered for type '{typeof(FormClassWithMultipleConstructors).FullName}'.", exception.Message);
    }

    [Fact]
    public void CreatePreservesConverterFailureForFormStructWithMultiplePublicConstructors()
    {
        static void TestAction([FromForm] FormStructWithMultipleConstructors value) { }

        var exception = Assert.Throws<InvalidOperationException>(() => RequestDelegateFactory.Create(TestAction));

        Assert.Equal($"No converter registered for type '{typeof(FormStructWithMultipleConstructors).FullName}'.", exception.Message);
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
