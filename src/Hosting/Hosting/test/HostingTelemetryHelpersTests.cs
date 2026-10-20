// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Hosting.Tests;

public class HostingTelemetryHelpersTests
{
    public static TheoryData<string, string> RedactedQueryStringData => new()
    {
        { "?", "" },
        { "??", "?" },
        { "??q=value", "?q=value" },
        { "?name=value", "name=value" },
        { "?first=one&second=two&third=three", "first=one&second=two&third=three" },
        { "?sig=secret&name=value", "sig=REDACTED&name=value" },
        { "?name=value&sig=secret&other=value", "name=value&sig=REDACTED&other=value" },
        { "?name=value&sig=secret", "name=value&sig=REDACTED" },
        { "?access_token=secret&Access_Token=value", "access_token=REDACTED&Access_Token=value" },
        {
            "?X-Amz-Signature=one&X-Amz-Credential=two&X-Amz-Security-Token=three&AWSAccessKeyId=four&Signature=five&sig=six&X-Goog-Signature=seven",
            "X-Amz-Signature=REDACTED&X-Amz-Credential=REDACTED&X-Amz-Security-Token=REDACTED&AWSAccessKeyId=four&Signature=five&sig=REDACTED&X-Goog-Signature=REDACTED"
        },
        { "?sig=one&sig=two&sig=three", "sig=REDACTED&sig=REDACTED&sig=REDACTED" },
        { "?safe=one&sig=two&safe=three&Signature=four", "safe=one&sig=REDACTED&safe=three&Signature=four" },
        { "?SIG=value&Sig=value&sIg=value", "SIG=value&Sig=value&sIg=value" },
        { "?x-amz-signature=value&X-AMZ-SIGNATURE=value", "x-amz-signature=value&X-AMZ-SIGNATURE=value" },
        { "?signature=value&AwsAccessKeyId=value&X-Goog-signature=value", "signature=value&AwsAccessKeyId=value&X-Goog-signature=value" },
        { "?sigil=value&mysig=value&sig-suffix=value&prefixSignature=value", "sigil=value&mysig=value&sig-suffix=value&prefixSignature=value" },
        { "?X-Amz-Signature2=value&2X-Amz-Signature=value", "X-Amz-Signature2=value&2X-Amz-Signature=value" },
        { "?sig&safe=value&Signature", "sig&safe=value&Signature" },
        { "?sig=&safe=&Signature=", "sig=REDACTED&safe=&Signature=" },
        { "?=value&==value&safe", "=value&==value&safe" },
        { "?&&sig=secret&&safe=value&&", "&&sig=REDACTED&&safe=value&&" },
        { "?&sig=secret", "&sig=REDACTED" },
        { "?sig=secret&", "sig=REDACTED&" },
        { "?safe=hello world&other=a+b&sig=top secret+plus", "safe=hello world&other=a+b&sig=REDACTED" },
        { "?%73ig=encoded-name", "%73ig=REDACTED" },
        { "?s%69g=encoded-name", "s%69g=REDACTED" },
        { "?%73%69%67=encoded-name", "%73%69%67=REDACTED" },
        { "?access%5Ftoken=encoded-name", "access%5Ftoken=REDACTED" },
        { "?X%2DAmz%2DSignature=encoded-name", "X%2DAmz%2DSignature=REDACTED" },
        { "?%58-Amz-Signature=encoded-name", "%58-Amz-Signature=REDACTED" },
        { "?%53ig=value&%73IG=value", "%53ig=value&%73IG=value" },
        { "?%2573ig=encoded-twice", "%2573ig=encoded-twice" },
        { "?si%26g=value&si%3Dg=value", "si%26g=value&si%3Dg=value" },
        { "?safe%26sig=value&safe%3Dsig=value", "safe%26sig=value&safe%3Dsig=value" },
        { "?sig%26ignored=value&sig%3Dignored=value", "sig%26ignored=value&sig%3Dignored=value" },
        { "?sig=%26safe%3Dvisible&safe=%73%65%63%72%65%74", "sig=REDACTED&safe=%73%65%63%72%65%74" },
        { "?si%=value&s%ZZig=value&safe=%", "si%=value&s%ZZig=value&safe=%" },
        { "?s%FFig=value&s%C3%28ig=value", "s%FFig=value&s%C3%28ig=value" },
        { "?s+ig=value&safe+name=a+b", "s+ig=value&safe+name=a+b" },
        { "?sig=secret#fragment", "sig=REDACTED" },
        { "?safe=value#fragment&sig=secret", "safe=value#fragment&sig=REDACTED" },
        { "?https://example.test/path?sig=not-a-key&sig=secret", "https://example.test/path?sig=not-a-key&sig=REDACTED" },
        { "?safe=REDACTED&sig=REDACTED", "safe=REDACTED&sig=REDACTED" },
        { "?ключ=значение&emoji=😀&sig=секрет", "ключ=значение&emoji=😀&sig=REDACTED" },
    };

    [Theory]
    [MemberData(nameof(RedactedQueryStringData))]
    public void GetRedactedQueryString_RedactsSensitiveValuesAndPreservesQueryText(string queryString, string expected)
    {
        Assert.Equal(expected, HostingTelemetryHelpers.GetRedactedQueryString(queryString));
    }

    [Fact]
    public void GetRedactedQueryString_VeryLongValues_RedactsOnlySensitiveValue()
    {
        var safeValue = new string('a', 16_384);
        var sensitiveValue = new string('b', 16_384);
        var queryString = $"?safe={safeValue}&sig={sensitiveValue}&after=value";

        var result = HostingTelemetryHelpers.GetRedactedQueryString(queryString);

        Assert.Equal($"safe={safeValue}&sig=REDACTED&after=value", result);
    }
}
