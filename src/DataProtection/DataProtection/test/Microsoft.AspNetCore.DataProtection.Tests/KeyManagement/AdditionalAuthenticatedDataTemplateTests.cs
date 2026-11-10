// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.DataProtection.Tests.KeyManagement;
public class AdditionalAuthenticatedDataTemplateTests
{
    [Fact]
    public void AdditionalAuthenticatedDataTemplateBuildAadTemplateBytes_ReturnsSameResultAsPreviousImplementation()
    {
        var actualBytes = KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate.BuildAadTemplateBytes([
            "my sample string",
            "©®±µ¶", // exotic unicode characters (https://en.wikipedia.org/wiki/List_of_Unicode_characters)
            "my other sample string",
            // more than 128 utf-8 bytes string
            "CfDJ8H5oH_fp1QNBmvs-OWXxsVoV30hrXeI4-PI4p1VZytjsgd0DTstMdtTZbFtm2dKHvsBlDCv7TiEWKztZf8fb48pUgBgUE2SeYV3eOUXvSfNWU0D8SmHLy5KEnwKKkZKqudDhCnjQSIU7mhDliJJN1e4",
            "."
        ]);

        // expected bytes are formed by running former code with the same input
        // former code can be found in https://github.com/dotnet/aspnetcore/pull/59322
        var expectedBytesInBase64 = "CfDJ8AAAAAAAAAAAAAAAAAAAAAAAAAAFEG15IHNhbXBsZSBzdHJpbmcKwqnCrsKxwrXCthZteSBvdGhlciBzYW1wbGUgc3RyaW5nmwFDZkRKOEg1b0hfZnAxUU5CbXZzLU9XWHhzVm9WMzBoclhlSTQtUEk0cDFWWnl0anNnZDBEVHN0TWR0VFpiRnRtMmRLSHZzQmxEQ3Y3VGlFV0t6dFpmOGZiNDhwVWdCZ1VFMlNlWVYzZU9VWHZTZk5XVTBEOFNtSEx5NUtFbndLS2taS3F1ZERoQ25qUVNJVTdtaERsaUpKTjFlNAEu";

        var actualBytesInBase64 = Convert.ToBase64String(actualBytes);
        Assert.Equal(expectedBytesInBase64, actualBytesInBase64);
    }

    [Fact]
    public void AdditionalAuthenticatedDataTemplateBuildAadTemplateBytes_ThrowsOnIllegalUtf8Text()
    {
        Assert.Throws<EncoderFallbackException>(() =>
        {
            var actualBytes = KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate.BuildAadTemplateBytes([
                "😀"[0] + "X",
            ]);
        });
    }

    [Fact]
    public void CreateProtector_IllegalUtf8Purpose_ThrowsEagerly()
    {
        var provider = new KeyRingBasedDataProtectionProvider(new Mock<IKeyRingProvider>(MockBehavior.Strict).Object, NullLoggerFactory.Instance);
        var illegalPurpose = "😀"[0] + "X";

        Assert.Throws<EncoderFallbackException>(() => provider.CreateProtector(illegalPurpose));
        Assert.Throws<EncoderFallbackException>(() => provider.CreateProtector("valid").CreateProtector(illegalPurpose));
    }

    [Fact]
    public void GetAadForKey_BuildsTemplateLazily_AndMatchesEagerlyBuiltTemplate()
    {
        string[] purposes = ["purpose1", "©®±µ¶", "purpose3"];
        var keyId = new Guid("ba73c9ce-d322-4e45-af90-341307e11c38");
        var expected = KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate.BuildAadTemplateBytes(purposes);
        Buffer.BlockCopy(keyId.ToByteArray(), 0, expected, sizeof(uint), 16);

        var template = new KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate(purposes);
        var protectAad = template.GetAadForKey(keyId, isProtecting: true);
        var unprotectAad = template.GetAadForKey(keyId, isProtecting: false);

        Assert.Equal(expected, protectAad);
        Assert.Same(protectAad, unprotectAad); // template was cached for the default key
    }

    [Fact]
    public async Task GetAadForKey_ConcurrentFirstUse_ProducesSameAad()
    {
        string[] purposes = ["a", "b", "c"];
        var keyId = Guid.NewGuid();
        var expected = KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate.BuildAadTemplateBytes(purposes);
        Buffer.BlockCopy(keyId.ToByteArray(), 0, expected, sizeof(uint), 16);

        for (var iteration = 0; iteration < 20; iteration++)
        {
            var holder = new TemplateHolder { Template = new KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate(purposes) };
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => Task.Run(() => holder.Template.GetAadForKey(keyId, isProtecting: i % 2 == 0))));
            Assert.All(results, aad => Assert.Equal(expected, aad));
        }
    }

    private sealed class TemplateHolder
    {
        public KeyRingBasedDataProtector.AdditionalAuthenticatedDataTemplate Template;
    }
}
