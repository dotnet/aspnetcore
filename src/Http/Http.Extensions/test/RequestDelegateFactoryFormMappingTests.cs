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
