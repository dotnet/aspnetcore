// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web.Extensions
{
    public class ProtectedBrowserStorageTest
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void RequiresStoreName(string storeName)
        {
            var ex = Assert.Throws<ArgumentException>(
                () => new TestProtectedBrowserStorage(storeName, new TestJSRuntime(), new TestDataProtectionProvider()));
            Assert.Equal("storeName", ex.ParamName);
        }

        [Fact]
        public void RequiresJSRuntime()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new TestProtectedBrowserStorage("someStoreName", null, new TestDataProtectionProvider()));
            Assert.Equal("jsRuntime", ex.ParamName);
        }

        [Fact]
        public void RequiresDataProtectionProvider()
        {
            var ex = Assert.Throws<ArgumentNullException>(
                () => new TestProtectedBrowserStorage("someStoreName", new TestJSRuntime(), null));
            Assert.Equal("dataProtectionProvider", ex.ParamName);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my custom purpose")]
        public void SetAsync_ProtectsAndInvokesJS(string customPurpose)
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var jsResultTask = new ValueTask<object>((object)null);
            var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
            var keyName = "test key";
            var expectedPurpose = customPurpose == null
                ? $"{typeof(TestProtectedBrowserStorage).FullName}:test store:{keyName}"
                : customPurpose;

            // Act
            jsRuntime.NextInvocationResult = jsResultTask;
            var result = customPurpose == null
                ? protectedBrowserStorage.SetAsync(keyName, data)
                : protectedBrowserStorage.SetAsync(customPurpose, keyName, data);

            // Assert
            var invocation = jsRuntime.Invocations.Single();
            Assert.Equal("Blazor._internal.protectedBrowserStorage.set", invocation.Identifier);
            Assert.Collection(invocation.Args,
                arg => Assert.Equal("test store", arg),
                arg => Assert.Equal(keyName, arg),
                arg => Assert.Equal(
                    "{\"StringProperty\":\"Hello\",\"IntProperty\":123}",
                    TestDataProtectionProvider.Unprotect(expectedPurpose, (string)arg)));
        }

        [Fact]
        public void SetAsync_ProtectsAndInvokesJS_NullValue()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var jsResultTask = new ValueTask<object>((object)null);
            var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:test store:test key";

            // Act
            jsRuntime.NextInvocationResult = jsResultTask;
            var result = protectedBrowserStorage.SetAsync("test key", null);

            // Assert
            var invocation = jsRuntime.Invocations.Single();
            Assert.Equal("Blazor._internal.protectedBrowserStorage.set", invocation.Identifier);
            Assert.Collection(invocation.Args,
                arg => Assert.Equal("test store", arg),
                arg => Assert.Equal("test key", arg),
                arg => Assert.Equal(
                    "null",
                    TestDataProtectionProvider.Unprotect(expectedPurpose, (string)arg)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my custom purpose")]
        public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_ValidData(string customPurpose)
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
            var keyName = "test key";
            var expectedPurpose = customPurpose == null
                ? $"{typeof(TestProtectedBrowserStorage).FullName}:test store:{keyName}"
                : customPurpose;
            var storedJson = "{\"StringProperty\":\"Hello\",\"IntProperty\":123}";
            jsRuntime.NextInvocationResult = new ValueTask<string>(
                TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

            // Act
            var result = customPurpose == null
                ? await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>(keyName)
                : await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>(customPurpose, keyName);

            // Assert
            Assert.Equal("Hello", result.StringProperty);
            Assert.Equal(123, result.IntProperty);

            var invocation = jsRuntime.Invocations.Single();
            Assert.Equal("Blazor._internal.protectedBrowserStorage.get", invocation.Identifier);
            Assert.Collection(invocation.Args,
                arg => Assert.Equal("test store", arg),
                arg => Assert.Equal(keyName, arg));
        }

        [Fact]
        public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_NoValue()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            jsRuntime.NextInvocationResult = new ValueTask<string>((string)null);

            // Act
            var result = await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>("test key");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_InvalidJson()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:test store:test key";
            var storedJson = "you can't parse this";
            jsRuntime.NextInvocationResult = new ValueTask<string>(
                TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

            // Act/Assert
            var ex = await Assert.ThrowsAsync<JsonException>(
                async () => await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>("test key"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_InvalidProtection(bool base64Encode)
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var storedString = "This string is not even protected";

            if (base64Encode)
            {
                // DataProtection deals with strings by base64-encoding the results.
                // Depending on whether the stored data is base64-encoded or not,
                // it will trigger a different failure point in data protection.
                storedString = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(storedString));
            }

            jsRuntime.NextInvocationResult = new ValueTask<string>(storedString);

            // Act/Assert
            var ex = await Assert.ThrowsAsync<CryptographicException>(
                async () => await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>("test key"));
        }

        [Fact]
        public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_WrongPurpose()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:test store:test key";
            var storedJson = "we won't even try to parse this";
            jsRuntime.NextInvocationResult = new ValueTask<string>(
                TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

            // Act/Assert
            var ex = await Assert.ThrowsAsync<CryptographicException>(
                async () => await protectedBrowserStorage.GetValueOrDefaultAsync<TestModel>("different key"));
            var innerException = ex.InnerException;
            Assert.IsType<ArgumentException>(innerException);
            Assert.Contains("The value is not protected with the expected purpose", innerException.Message);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("my custom purpose")]
        public async Task TryGetAsync_InvokesJSAndUnprotects_ValidData(string customPurpose)
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
            var keyName = "test key";
            var expectedPurpose = customPurpose ?? $"{typeof(TestProtectedBrowserStorage).FullName}:test store:{keyName}";
            var storedJson = "{\"StringProperty\":\"Hello\",\"IntProperty\":123}";
            jsRuntime.NextInvocationResult = new ValueTask<string>(
                TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

            // Act
            var result = customPurpose == null
                ? await protectedBrowserStorage.TryGetAsync<TestModel>(keyName)
                : await protectedBrowserStorage.TryGetAsync<TestModel>(customPurpose, keyName);

            // Assert
            Assert.True(result.success);
            Assert.Equal("Hello", result.result.StringProperty);
            Assert.Equal(123, result.result.IntProperty);

            var invocation = jsRuntime.Invocations.Single();
            Assert.Equal("Blazor._internal.protectedBrowserStorage.get", invocation.Identifier);
            Assert.Collection(invocation.Args,
                arg => Assert.Equal("test store", arg),
                arg => Assert.Equal(keyName, arg));
        }

        [Fact]
        public async Task TryGetAsync_InvokesJSAndUnprotects_NoValue()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            jsRuntime.NextInvocationResult = new ValueTask<string>((string)null);

            // Act
            var result = await protectedBrowserStorage.TryGetAsync<TestModel>("test key");

            // Assert
            Assert.False(result.success);
            Assert.Null(result.result);
        }

        [Fact]
        public void DeleteAsync_InvokesJS()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);
            var nextTask = new ValueTask<object>((object)null);
            jsRuntime.NextInvocationResult = nextTask;

            // Act
            var result = protectedBrowserStorage.DeleteAsync("test key");

            // Assert
            var invocation = jsRuntime.Invocations.Single();
            Assert.Equal("Blazor._internal.protectedBrowserStorage.delete", invocation.Identifier);
            Assert.Collection(invocation.Args,
                arg => Assert.Equal("test store", arg),
                arg => Assert.Equal("test key", arg));
        }

        [Fact]
        public async Task ReusesCachedProtectorsByPurpose()
        {
            // Arrange
            var jsRuntime = new TestJSRuntime();
            jsRuntime.NextInvocationResult = new ValueTask<object>((object)null);
            var dataProtectionProvider = new TestDataProtectionProvider();
            var protectedBrowserStorage = new TestProtectedBrowserStorage("test store", jsRuntime, dataProtectionProvider);

            // Act
            await protectedBrowserStorage.SetAsync("key 1", null);
            await protectedBrowserStorage.SetAsync("key 2", null);
            await protectedBrowserStorage.SetAsync("key 1", null);
            await protectedBrowserStorage.SetAsync("key 3", null);

            // Assert
            var typeName = typeof(TestProtectedBrowserStorage).FullName;
            var expectedPurposes = new[]
            {
                $"{typeName}:test store:key 1",
                $"{typeName}:test store:key 2",
                $"{typeName}:test store:key 3"
            };
            Assert.Equal(expectedPurposes, dataProtectionProvider.ProtectorsCreated.ToArray());

            Assert.Collection(jsRuntime.Invocations,
                invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[0], "null"), invocation.Args[2]),
                invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[1], "null"), invocation.Args[2]),
                invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[0], "null"), invocation.Args[2]),
                invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[2], "null"), invocation.Args[2]));
        }

        class TestModel
        {
            public string StringProperty { get; set; }

            public int IntProperty { get; set; }
        }

        class TestDataProtectionProvider : IDataProtectionProvider
        {
            public List<string> ProtectorsCreated { get; } = new List<string>();


            public static string Protect(string purpose, string plaintext)
                => new TestDataProtector(purpose).Protect(plaintext);

            public static string Unprotect(string purpose, string protectedValue)
                => new TestDataProtector(purpose).Unprotect(protectedValue);

            public IDataProtector CreateProtector(string purpose)
            {
                ProtectorsCreated.Add(purpose);
                return new TestDataProtector(purpose);
            }

            class TestDataProtector : IDataProtector
            {
                private readonly string _purpose;

                public TestDataProtector(string purpose)
                {
                    _purpose = purpose;
                }

                public IDataProtector CreateProtector(string purpose)
                {
                    throw new NotImplementedException();
                }

                public byte[] Protect(byte[] plaintext)
                {
                    // The test cases will only involve passing data that was originally converted from strings
                    var plaintextString = Encoding.UTF8.GetString(plaintext);
                    var fakeProtectedString = $"{ProtectionPrefix(_purpose)}{plaintextString}";
                    return Encoding.UTF8.GetBytes(fakeProtectedString);
                }

                public byte[] Unprotect(byte[] protectedData)
                {
                    // The test cases will only involve passing data that was originally converted from strings
                    var protectedString = Encoding.UTF8.GetString(protectedData);

                    var expectedPrefix = ProtectionPrefix(_purpose);
                    if (!protectedString.StartsWith(expectedPrefix, StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"The value is not protected with the expected purpose '{_purpose}'. Value supplied: '{protectedString}'", nameof(protectedData));
                    }

                    var unprotectedString = protectedString.Substring(expectedPrefix.Length);
                    return Encoding.UTF8.GetBytes(unprotectedString);
                }

                private static string ProtectionPrefix(string purpose)
                    => $"PROTECTED:{purpose}:";
            }
        }

        class TestJSRuntime : IJSRuntime
        {
            public List<(string Identifier, object[] Args)> Invocations { get; }
                = new List<(string Identifier, object[] Args)>();

            public object NextInvocationResult { get; set; }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args)
            {
                Invocations.Add((identifier, args));
                return (ValueTask<TValue>)NextInvocationResult;
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args)
                => InvokeAsync<TValue>(identifier, cancellationToken: CancellationToken.None, args: args);
        }

        class TestProtectedBrowserStorage : ProtectedBrowserStorage
        {
            public TestProtectedBrowserStorage(string storeName, IJSRuntime jsRuntime, IDataProtectionProvider dataProtectionProvider)
                : base(storeName, jsRuntime, dataProtectionProvider)
            {
            }
        }
    }
}
