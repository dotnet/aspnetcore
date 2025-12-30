// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Http.Tests;

public class CookieOptionsTests
{
    [Fact]
    public void CopyCtor_AllPropertiesCopied()
    {
        var original = new CookieOptions()
        {
            Domain = "domain",
            Expires = DateTime.UtcNow,
            Extensions = { "ext1", "ext2=v2" },
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(10),
            Path = "/foo",
            Secure = true,
            SameSite = SameSiteMode.Strict,
        };
        var copy = new CookieOptions(original);

        var properties = typeof(CookieOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            switch (property.Name)
            {
                case "Domain":
                case "Expires":
                case "HttpOnly":
                case "IsEssential":
                case "MaxAge":
                case "Path":
                case "Secure":
                case "SameSite":
                    Assert.Equal(property.GetValue(original), property.GetValue(copy));
                    break;
                case "Extensions":
                    Assert.NotSame(property.GetValue(original), property.GetValue(copy));
                    break;
                default:
                    Assert.Fail("Not implemented: " + property.Name);
                    break;
            }
        }

        Assert.Equal(original.Extensions.Count, copy.Extensions.Count);
        foreach (var value in original.Extensions)
        {
            Assert.Contains(value, copy.Extensions);
        }
    }
}
