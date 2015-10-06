// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.ViewComponents
{
    public class ViewComponentMethodSelectorTest
    {
        [Theory]
        [InlineData(typeof(ViewComponentWithSyncInvoke), new object[] { "" })]
        [InlineData(typeof(ViewComponentWithAsyncInvoke), new object[] { 42, false })]
        [InlineData(typeof(ViewComponentWithNonPublicNonInstanceInvokes), new object[] { })]
        [InlineData(typeof(ViewComponentWithNonPublicNonInstanceInvokes), new object[] { "" })]
        public void FindAsyncMethod_ReturnsNull_IfMatchCannotBeFound(Type type, object[] args)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();

            // Act
            var method = ViewComponentMethodSelector.FindAsyncMethod(typeInfo, args);

            // Assert
            Assert.Null(method);
        }

        [Theory]
        [InlineData(typeof(ViewComponentWithAsyncInvoke), new object[0])]
        [InlineData(typeof(ViewComponentWithSyncInvoke), new object[] { "" })]
        [InlineData(typeof(ViewComponentWithAsyncInvoke), new object[] { "" })]
        [InlineData(typeof(ViewComponentWithSyncInvoke), new object[] { 42 })]
        [InlineData(typeof(ViewComponentWithAsyncInvoke), new object[] { "", 42 })]
        [InlineData(typeof(ViewComponentWithNonPublicNonInstanceInvokes), new object[] { })]
        [InlineData(typeof(ViewComponentWithNonPublicNonInstanceInvokes), new object[] { "" })]
        [InlineData(typeof(BaseClass), new object[] { })]
        public void FindSyncMethod_ReturnsNull_IfMatchCannotBeFound(Type type, object[] args)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();

            // Act
            var method = ViewComponentMethodSelector.FindSyncMethod(typeInfo, args);

            // Assert
            Assert.Null(method);
        }

        [Theory]
        [InlineData(new object[] { new object[] { "Hello" } })]
        [InlineData(new object[] { new object[] { 4 } })]
        [InlineData(new object[] { new object[] { "", 5 } })]
        public void FindAsyncMethod_ThrowsIfInvokeAsyncDoesNotHaveCorrectReturnType(object[] args)
        {
            // Arrange
            var typeInfo = typeof(TypeWithInvalidInvokeAsync).GetTypeInfo();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ViewComponentMethodSelector.FindAsyncMethod(typeInfo, args));
            Assert.Equal("The async view component method 'InvokeAsync' should be declared to return Task<T>.",
                ex.Message);
        }

        [Fact]
        public void FindSyncMethod_ThrowsIfInvokeSyncIsAVoidMethod()
        {
            // Arrange
            var expectedMessage = "The view component method 'Invoke' should be declared to return a value.";
            var typeInfo = typeof(TypeWithInvalidInvokeSync).GetTypeInfo();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ViewComponentMethodSelector.FindSyncMethod(typeInfo, new object[] { 4 }));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void FindSyncMethod_ThrowsIfInvokeSyncReturnsTask()
        {
            // Arrange
            var expectedMessage = "The view component method 'Invoke' cannot return a Task.";
            var typeInfo = typeof(TypeWithInvalidInvokeSync).GetTypeInfo();

            // Act and Assert
            var ex = Assert.Throws<InvalidOperationException>(
                () => ViewComponentMethodSelector.FindSyncMethod(typeInfo, new object[] { "" }));
            Assert.Equal(expectedMessage, ex.Message);
        }

        public static TheoryData FindAsyncMethod_ReturnsMethodMatchingParametersData
        {
            get
            {
                var derivedClass = new DerivedClass();

                return new TheoryData<Type, object[], string>
                {
                    { typeof(ViewComponentWithAsyncInvoke), new object[] { "", }, "1" },
                    { typeof(ViewComponentWithAsyncInvoke), new object[] { "", 2 }, "2" },
                    { typeof(ViewComponentWithAsyncInvoke), new object[] { "", 0, 1 }, "3" },
                    { typeof(ViewComponentWithAsyncInvoke), new object[] { 1, false, 1 }, "4" },
                    { typeof(MethodsWithValueConversions), new object[] { 2, (byte)1, (byte)2 }, "2" },
                    { typeof(MethodsWithValueConversions), new object[] { derivedClass, derivedClass }, "4" },
                    { typeof(MethodsWithValueConversions), new object[] { CultureInfo.InvariantCulture }, "6" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(FindAsyncMethod_ReturnsMethodMatchingParametersData))]
        public void FindAsyncMethod_ReturnsMethodMatchingParameters(Type type, object[] args, string expectedId)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();

            // Act
            var method = ViewComponentMethodSelector.FindAsyncMethod(typeInfo, args);

            // Assert
            Assert.NotNull(method);
            var data = method.GetCustomAttribute<MethodDataAttribute>();
            Assert.Equal(expectedId, data.Data);
        }

        public static TheoryData FindSyncMethod_ReturnsMethodMatchingParametersData
        {
            get
            {
                var derivedClass = new DerivedAgain();

                return new TheoryData<Type, object[], string>
                {
                    { typeof(ViewComponentWithSyncInvoke), new object[] { }, "1" },
                    { typeof(ViewComponentWithSyncInvoke), new object[] { 2, 3 }, "2" },
                    { typeof(ViewComponentWithSyncInvoke), new object[] { "", 0, true }, "3" },
                    { typeof(MethodsWithValueConversions), new object[] { 1, (byte)2, 3.0f }, "1" },
                    { typeof(MethodsWithValueConversions), new object[] { derivedClass, derivedClass }, "3" },
                    { typeof(MethodsWithValueConversions), new object[] { "Hello world" }, "5" },
                    { typeof(DerivedClass), new object[] { }, "Derived1" },
#if !DNXCORE50
                    { typeof(DerivedAgain), new object[] { "" }, "Derived2" },
#endif
                };
            }
        }

        [Theory]
        [MemberData(nameof(FindSyncMethod_ReturnsMethodMatchingParametersData))]
        public void FindSyncMethod_ReturnsMethodMatchingParameters(Type type, object[] args, string expectedId)
        {
            // Arrange
            var typeInfo = type.GetTypeInfo();

            // Act
            var method = ViewComponentMethodSelector.FindSyncMethod(typeInfo, args);

            // Assert
            Assert.NotNull(method);
            var data = method.GetCustomAttribute<MethodDataAttribute>();
            Assert.Equal(expectedId, data.Data);
        }

        private class ViewComponentWithSyncInvoke
        {
            [MethodData("1")]
            public int Invoke() => 3;

            [MethodData("2")]
            public int Invoke(int a, int? b) => a + b.Value;

            [MethodData("3")]
            public int Invoke(string a, int b, bool? c) => 3;
        }

        private class ViewComponentWithAsyncInvoke
        {
            [MethodData("1")]
            public Task<string> InvokeAsync(string value) => Task.FromResult(value.ToUpperInvariant());

            [MethodData("2")]
            public Task<string> InvokeAsync(string a, int b) => Task.FromResult(a + b);

            [MethodData("3")]
            public Task<string> InvokeAsync(string a, int? b, int c) => Task.FromResult(a + b + c);

            [MethodData("4")]
            public Task<string> InvokeAsync(int? a, bool? b, int c) => Task.FromResult(a.ToString() + b + c);

            [MethodData("5")]
            public Task<string> InvokeAsync(object value) => Task.FromResult(value.ToString());
        }

        public class MethodsWithValueConversions
        {
            [MethodData("1")]
            public int Invoke(long a, char b, double c) => 1;

            [MethodData("2")]
            public Task<int> InvokeAsync(float a, float b, byte c) => Task.FromResult(1);

            [MethodData("3")]
            public int Invoke(BaseClass a, DerivedClass b) => 1;

            [MethodData("4")]
            public Task<int> InvokeAsync(BaseClass a, DerivedClass b) => Task.FromResult(1);

            [MethodData("5")]
            public int Invoke(IEnumerable<char> value) => 1;

            [MethodData("6")]
            public Task<int> InvokeAsync(IFormatProvider formatProvider) => Task.FromResult(1);
        }

        private class ViewComponentWithNonPublicNonInstanceInvokes
        {
            public static int Invoke() => 1;

            private int Invoke(string a) => 2;

            public static Task<int> InvokeAsync() => Task.FromResult(3);

            protected Task<string> InvokeAsync(string a) => Task.FromResult(a);
        }

        public class BaseClass
        {
            [MethodData("Base")]
            public static int Invoke() => 1;
        }

        public class DerivedClass : BaseClass
        {
            [MethodData("Derived1")]
            public new int Invoke() => 1;

            [MethodData("Derived2")]
            public int Invoke(string x) => 2;
        }

        public class DerivedAgain : DerivedClass
        {
            [MethodData("DerivedAgain")]
            public new static int Invoke(string x) => 2;
        }

        private class TypeWithInvalidInvokeAsync
        {
            public Task InvokeAsync(string value) => Task.FromResult(value);

            public void InvokeAsync(int value)
            {

            }

            public long InvokeAsync(string a, int b) => b;
        }

        private class TypeWithInvalidInvokeSync
        {
            public Task Invoke(string value) => Task.FromResult(value);

            public void Invoke(int value)
            {
            }
        }

        private class MethodDataAttribute : Attribute
        {
            public MethodDataAttribute(string data)
            {
                Data = data;
            }

            public string Data { get; }
        }
    }
}
