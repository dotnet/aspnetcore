// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;

namespace Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

public class ProtectedBrowserStorageTest
{
    [Fact]
    public void SetAsync_ProtectsAndInvokesJS_DefaultPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var jsResultTask = new ValueTask<object>((object)null);
        var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
        var keyName = "testKey";
        var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:testStore:{keyName}";

        // Act
        jsRuntime.NextInvocationResult = jsResultTask;
        var result = protectedBrowserStorage.SetAsync(keyName, data);

        // Assert
        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.setItem", invocation.Identifier);
        Assert.Collection(invocation.Args,
            arg => Assert.Equal(keyName, arg),
            arg => Assert.Equal(
                "{\"stringProperty\":\"Hello\",\"intProperty\":123}",
                TestDataProtectionProvider.Unprotect(expectedPurpose, (string)arg)));
    }

    [Fact]
    public void SetAsync_ProtectsAndInvokesJS_CustomPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var jsResultTask = new ValueTask<object>((object)null);
        var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
        var keyName = "testKey";
        var customPurpose = "my custom purpose";

        // Act
        jsRuntime.NextInvocationResult = jsResultTask;
        var result = protectedBrowserStorage.SetAsync(customPurpose, keyName, data);

        // Assert
        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.setItem", invocation.Identifier);
        Assert.Collection(invocation.Args,
            arg => Assert.Equal(keyName, arg),
            arg => Assert.Equal(
                "{\"stringProperty\":\"Hello\",\"intProperty\":123}",
                TestDataProtectionProvider.Unprotect(customPurpose, (string)arg)));
    }

    [Fact]
    public void SetAsync_ProtectsAndInvokesJS_NullValue()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var jsResultTask = new ValueTask<object>((object)null);
        var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:testStore:testKey";

        // Act
        jsRuntime.NextInvocationResult = jsResultTask;
        var result = protectedBrowserStorage.SetAsync("testKey", null);

        // Assert
        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.setItem", invocation.Identifier);
        Assert.Collection(invocation.Args,
            arg => Assert.Equal("testKey", arg),
            arg => Assert.Equal(
                "null",
                TestDataProtectionProvider.Unprotect(expectedPurpose, (string)arg)));
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_ValidData_DefaultPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
        var keyName = "testKey";
        var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:testStore:{keyName}";
        var storedJson = "{\"StringProperty\":\"Hello\",\"IntProperty\":123}";
        jsRuntime.NextInvocationResult = new ValueTask<string>(
            TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

        // Act
        var result = await protectedBrowserStorage.GetAsync<TestModel>(keyName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello", result.Value.StringProperty);
        Assert.Equal(123, result.Value.IntProperty);

        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.getItem", invocation.Identifier);
        Assert.Collection(invocation.Args, arg => Assert.Equal(keyName, arg));
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_ValidData_CustomPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var data = new TestModel { StringProperty = "Hello", IntProperty = 123 };
        var keyName = "testKey";
        var customPurpose = "my custom purpose";
        var storedJson = "{\"StringProperty\":\"Hello\",\"IntProperty\":123}";
        jsRuntime.NextInvocationResult = new ValueTask<string>(
            TestDataProtectionProvider.Protect(customPurpose, storedJson));

        // Act
        var result = await protectedBrowserStorage.GetAsync<TestModel>(customPurpose, keyName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello", result.Value.StringProperty);
        Assert.Equal(123, result.Value.IntProperty);

        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.getItem", invocation.Identifier);
        Assert.Collection(invocation.Args, arg => Assert.Equal(keyName, arg));
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_NoValue()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        jsRuntime.NextInvocationResult = new ValueTask<string>((string)null);

        // Act
        var result = await protectedBrowserStorage.GetAsync<TestModel>("testKey");

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_InvalidJson()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:testStore:testKey";
        var storedJson = "you can't parse this";
        jsRuntime.NextInvocationResult = new ValueTask<string>(
            TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

        // Act/Assert
        var ex = await Assert.ThrowsAsync<JsonException>(
            async () => await protectedBrowserStorage.GetAsync<TestModel>("testKey"));
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_InvalidProtection_Plaintext()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var storedString = "This string is not even protected";

        jsRuntime.NextInvocationResult = new ValueTask<string>(storedString);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<CryptographicException>(
            async () => await protectedBrowserStorage.GetAsync<TestModel>("testKey"));
    }

    [Fact]
    public async Task GetAsync_InvokesJSAndUnprotects_InvalidProtection_Base64Encoded()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);

        // DataProtection deals with strings by base64-encoding the results.
        // Depending on whether the stored data is base64-encoded or not,
        // it will trigger a different failure point in data protection.
        var storedString = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("This string is not even protected"));

        jsRuntime.NextInvocationResult = new ValueTask<string>(storedString);

        // Act/Assert
        var ex = await Assert.ThrowsAsync<CryptographicException>(
            async () => await protectedBrowserStorage.GetAsync<TestModel>("testKey"));
    }

    [Fact]
    public async Task GetValueOrDefaultAsync_InvokesJSAndUnprotects_WrongPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var expectedPurpose = $"{typeof(TestProtectedBrowserStorage).FullName}:testStore:testKey";
        var storedJson = "we won't even try to parse this";
        jsRuntime.NextInvocationResult = new ValueTask<string>(
            TestDataProtectionProvider.Protect(expectedPurpose, storedJson));

        // Act/Assert
        var ex = await Assert.ThrowsAsync<CryptographicException>(
            async () => await protectedBrowserStorage.GetAsync<TestModel>("different key"));
        var innerException = ex.InnerException;
        Assert.IsType<ArgumentException>(innerException);
        Assert.Contains("The value is not protected with the expected purpose", innerException.Message);
    }

    [Fact]
    public void DeleteAsync_InvokesJS()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);
        var nextTask = new ValueTask<object>((object)null);
        jsRuntime.NextInvocationResult = nextTask;

        // Act
        var result = protectedBrowserStorage.DeleteAsync("testKey");

        // Assert
        var invocation = jsRuntime.Invocations.Single();
        Assert.Equal("testStore.removeItem", invocation.Identifier);
        Assert.Collection(invocation.Args, arg => Assert.Equal("testKey", arg));
    }

    [Fact]
    public async Task ReusesCachedProtectorsByPurpose()
    {
        // Arrange
        var jsRuntime = new TestJSRuntime();
        jsRuntime.NextInvocationResult = new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>());
        var dataProtectionProvider = new TestDataProtectionProvider();
        var protectedBrowserStorage = new TestProtectedBrowserStorage("testStore", jsRuntime, dataProtectionProvider);

        // Act
        await protectedBrowserStorage.SetAsync("key 1", null);
        await protectedBrowserStorage.SetAsync("key 2", null);
        await protectedBrowserStorage.SetAsync("key 1", null);
        await protectedBrowserStorage.SetAsync("key 3", null);

        // Assert
        var typeName = typeof(TestProtectedBrowserStorage).FullName;
        var expectedPurposes = new[]
        {
                $"{typeName}:testStore:key 1",
                $"{typeName}:testStore:key 2",
                $"{typeName}:testStore:key 3"
            };
        Assert.Equal(expectedPurposes, dataProtectionProvider.ProtectorsCreated.ToArray());

        Assert.Collection(jsRuntime.Invocations,
            invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[0], "null"), invocation.Args[1]),
            invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[1], "null"), invocation.Args[1]),
            invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[0], "null"), invocation.Args[1]),
            invocation => Assert.Equal(TestDataProtectionProvider.Protect(expectedPurposes[2], "null"), invocation.Args[1]));
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
