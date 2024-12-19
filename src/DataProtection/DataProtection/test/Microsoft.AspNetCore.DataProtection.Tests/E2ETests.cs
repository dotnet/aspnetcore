// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.Tests;
public class E2ETests
{
    [Fact]
    public void ProtectAndUnprotect_ForSampleAntiforgeryToken()
    {
        const string sampleToken = "CfDJ8H5oH_fp1QNBmvs-OWXxsVoV30hrXeI4-PI4p1VZytjsgd0DTstMdtTZbFtm2dKHvsBlDCv7TiEWKztZf8fb48pUgBgUE2SeYV3eOUXvSfNWU0D8SmHLy5KEnwKKkZKqudDhCnjQSIU7mhDliJJN1e4";

        var dataProtector = GetServiceCollectionBuiltDataProtector();
        var encrypted = dataProtector.Protect(sampleToken);
        var decrypted = dataProtector.Unprotect(encrypted);
        Assert.Equal(sampleToken, decrypted);
    }

    private static IDataProtector GetServiceCollectionBuiltDataProtector(string purpose = "samplePurpose")
        => new ServiceCollection()
            .AddDataProtection()
            .Services.BuildServiceProvider()
            .GetDataProtector(purpose);
}
