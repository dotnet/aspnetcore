// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;

namespace Microsoft.JSInterop.Test
{
    public class DotNetDispatcherTest
    {
        private readonly static string thisAssemblyName
            = typeof(DotNetDispatcherTest).Assembly.GetName().Name;

        [Fact]
        public void CannotInvokeWithEmptyAssemblyName()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(" ", "SomeMethod", "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("assemblyName", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeWithEmptyMethodIdentifier()
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke("SomeAssembly", " ", "[]");
            });

            Assert.StartsWith("Cannot be null, empty, or whitespace.", ex.Message);
            Assert.Equal("methodIdentifier", ex.ParamName);
        }

        [Fact]
        public void CannotInvokeMethodsOnUnloadedAssembly()
        {
            var assemblyName = "Some.Fake.Assembly";
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(assemblyName, "SomeMethod", null);
            });

            Assert.Equal($"There is no loaded assembly with the name '{assemblyName}'.", ex.Message);
        }

        // Note: Currently it's also not possible to invoke instance or generic methods.
        // That's not something determined by DotNetDispatcher, but rather by the fact that we
        // don't pass any 'target' or close over the generics in the reflection code.
        // Not defining this behavior through unit tests because the default outcome is
        // fine (an exception stating what info is missing), plus we're likely to add support
        // for invoking instance methods in the near future.

        [Theory]
        [InlineData("MethodOnInternalType")]
        [InlineData("PrivateMethod")]
        [InlineData("ProtectedMethod")]
        [InlineData("MethodWithoutAttribute")] // That's not really its identifier; just making the point that there's no way to invoke it
        public void CannotInvokeUnsuitableMethods(string methodIdentifier)
        {
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(thisAssemblyName, methodIdentifier, null);
            });

            Assert.Equal($"The assembly '{thisAssemblyName}' does not contain a public method with [JSInvokableAttribute(\"{methodIdentifier}\")].", ex.Message);
        }

        [Fact]
        public void CanInvokeStaticVoidMethod()
        {
            // Arrange/Act
            SomePublicType.DidInvokeMyInvocableVoid = false;
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticVoid", null);

            // Assert
            Assert.Null(resultJson);
            Assert.True(SomePublicType.DidInvokeMyInvocableVoid);
        }

        [Fact]
        public void CanInvokeStaticNonVoidMethod()
        {
            // Arrange/Act
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticNonVoid", null);
            var result = Json.Deserialize<TestDTO>(resultJson);

            // Assert
            Assert.Equal("Test", result.StringVal);
            Assert.Equal(123, result.IntVal);
        }

        [Fact]
        public void CanInvokeStaticWithParams()
        {
            // Arrange
            var argsJson = Json.Serialize(new object[] {
                new TestDTO { StringVal = "Another string", IntVal = 456 },
                new[] { 100, 200 }
            });

            // Act
            var resultJson = DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticWithParams", argsJson);
            var result = Json.Deserialize<TestDTO>(resultJson);

            // Assert
            Assert.Equal("ANOTHER STRING", result.StringVal);
            Assert.Equal(756, result.IntVal);
        }

        [Fact]
        public void CannotInvokeWithIncorrectNumberOfParams()
        {
            // Arrange
            var argsJson = Json.Serialize(new object[] { 1, 2, 3, 4 });

            // Act/Assert
            var ex = Assert.Throws<ArgumentException>(() =>
            {
                DotNetDispatcher.Invoke(thisAssemblyName, "InvocableStaticWithParams", argsJson);
            });

            Assert.Equal("In call to 'InvocableStaticWithParams', expected 2 parameters but received 4.", ex.Message);
        }

        internal class SomeInteralType
        {
            [JSInvokable("MethodOnInternalType")] public void MyMethod() { }
        }

        public class SomePublicType
        {
            public static bool DidInvokeMyInvocableVoid;

            [JSInvokable("PrivateMethod")] private void MyPrivateMethod() { }
            [JSInvokable("ProtectedMethod")] protected void MyProtectedMethod() { }
            protected void MethodWithoutAttribute() { }

            [JSInvokable("InvocableStaticVoid")] public static void MyInvocableVoid()
            {
                DidInvokeMyInvocableVoid = true;
            }

            [JSInvokable("InvocableStaticNonVoid")]
            public static object MyInvocableNonVoid()
                => new TestDTO { StringVal = "Test", IntVal = 123 };

            [JSInvokable("InvocableStaticWithParams")]
            public static TestDTO MyInvocableWithParams(TestDTO dto, int[] incrementAmounts)
                => new TestDTO { StringVal = dto.StringVal.ToUpperInvariant(), IntVal = dto.IntVal + incrementAmounts.Sum() };
        }

        public class TestDTO
        {
            public string StringVal { get; set; }
            public int IntVal { get; set; }
        }
    }
}
