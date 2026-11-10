// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Authentication.DataHandler;

public class SecureDataFormatTests
{
    public SecureDataFormatTests()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDataProtection();
        ServiceProvider = serviceCollection.BuildServiceProvider();
    }

    public IServiceProvider ServiceProvider { get; }

    [Fact]
    public void ProtectDataRoundTrips()
    {
        var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var prototector = provider.CreateProtector("test");
        var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

        string input = "abcdefghijklmnopqrstuvwxyz0123456789";
        var protectedData = secureDataFormat.Protect(input);
        var result = secureDataFormat.Unprotect(protectedData);
        Assert.Equal(input, result);
    }

    [Fact]
    public void ProtectWithPurposeRoundTrips()
    {
        var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var prototector = provider.CreateProtector("test");
        var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

        string input = "abcdefghijklmnopqrstuvwxyz0123456789";
        string purpose = "purpose1";
        var protectedData = secureDataFormat.Protect(input, purpose);
        var result = secureDataFormat.Unprotect(protectedData, purpose);
        Assert.Equal(input, result);
    }

    [Fact]
    public void UnprotectWithDifferentPurposeFails()
    {
        var provider = ServiceProvider.GetRequiredService<IDataProtectionProvider>();
        var prototector = provider.CreateProtector("test");
        var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), prototector);

        string input = "abcdefghijklmnopqrstuvwxyz0123456789";
        string purpose = "purpose1";
        var protectedData = secureDataFormat.Protect(input, purpose);
        var result = secureDataFormat.Unprotect(protectedData); // Null other purpose
        Assert.Null(result);

        result = secureDataFormat.Unprotect(protectedData, "purpose2");
        Assert.Null(result);
    }

    [Fact]
    public void DefaultProtectorSupportsSpans()
    {
        var protector = ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");

        // The tests below rely on the default protector taking the span-based path.
        Assert.IsAssignableFrom<ISpanDataProtector>(protector);
        Assert.IsAssignableFrom<ISpanDataProtector>(protector.CreateProtector("purpose1"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(4096)]
    [InlineData(100_000)]
    public void SpanAndArrayProtectorsInteroperate(int length)
    {
        var protector = ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
        var spanFormat = new SecureDataFormat<string>(new StringSerializer(), protector);
        var arrayFormat = new SecureDataFormat<string>(new StringSerializer(), new ArrayOnlyDataProtector(protector));

        var input = new string('a', length) + "\u00e9\u4e2d";

        Assert.Equal(input, arrayFormat.Unprotect(spanFormat.Protect(input)));
        Assert.Equal(input, spanFormat.Unprotect(arrayFormat.Protect(input)));
        Assert.Equal(input, arrayFormat.Unprotect(spanFormat.Protect(input, "purpose1"), "purpose1"));
        Assert.Equal(input, spanFormat.Unprotect(arrayFormat.Protect(input, "purpose1"), "purpose1"));
    }

    [Fact]
    public void UnprotectAcceptsStandardBase64()
    {
        var protector = ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
        var secureDataFormat = new SecureDataFormat<string>(new StringSerializer(), protector);

        var input = "abcdefghijklmnopqrstuvwxyz0123456789";
        var found = false;
        for (var i = 0; i < 100 && !found; i++)
        {
            // Ciphertext is random, so retry until the standard base64 form contains characters base64url does not allow.
            var base64 = Convert.ToBase64String(protector.Protect(Encoding.UTF8.GetBytes(input)));
            if (base64.AsSpan().IndexOfAny('+', '/') >= 0)
            {
                found = true;
                Assert.Equal(input, secureDataFormat.Unprotect(base64));
            }
        }

        Assert.True(found);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("!!!!")]
    [InlineData("ab+c-d")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    public void UnprotectReturnsNullForInvalidInput(string protectedText)
    {
        var protector = ServiceProvider.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
        var spanFormat = new SecureDataFormat<string>(new StringSerializer(), protector);
        var arrayFormat = new SecureDataFormat<string>(new StringSerializer(), new ArrayOnlyDataProtector(protector));

        Assert.Null(spanFormat.Unprotect(protectedText));
        Assert.Null(arrayFormat.Unprotect(protectedText));
        Assert.Null(spanFormat.Unprotect(null));
    }

    private class ArrayOnlyDataProtector : IDataProtector
    {
        private readonly IDataProtector _inner;

        public ArrayOnlyDataProtector(IDataProtector inner) => _inner = inner;

        public IDataProtector CreateProtector(string purpose) => new ArrayOnlyDataProtector(_inner.CreateProtector(purpose));

        public byte[] Protect(byte[] plaintext) => _inner.Protect(plaintext);

        public byte[] Unprotect(byte[] protectedData) => _inner.Unprotect(protectedData);
    }

    private class StringSerializer : IDataSerializer<string>
    {
        public byte[] Serialize(string model)
        {
            return Encoding.UTF8.GetBytes(model);
        }

        public string Deserialize(byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
    }
}
