// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Hybrid.Internal;

namespace Microsoft.Extensions.Caching.Hybrid.Tests;
public class TypeTests
{
    [Theory]
    [InlineData(typeof(string))]
    [InlineData(typeof(int))] // primitive
    [InlineData(typeof(int?))]
    [InlineData(typeof(Guid))] // non-primitive but blittable
    [InlineData(typeof(Guid?))]
    [InlineData(typeof(SealedCustomClassAttribTrue))] // attrib says explicitly true, and sealed
    [InlineData(typeof(CustomBlittableStruct))] // blittable, and we're copying each time
    [InlineData(typeof(CustomNonBlittableStructAttribTrue))] // non-blittable, attrib says explicitly true
    public void ImmutableTypes(Type type)
    {
        Assert.True((bool)typeof(DefaultHybridCache.ImmutableTypeCache<>).MakeGenericType(type)
            .GetField(nameof(DefaultHybridCache.ImmutableTypeCache<string>.IsImmutable), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!);
    }

    [Theory]
    [InlineData(typeof(byte[]))]
    [InlineData(typeof(string[]))]
    [InlineData(typeof(object))]
    [InlineData(typeof(CustomClassNoAttrib))] // no attrib, who knows?
    [InlineData(typeof(CustomClassAttribFalse))] // attrib says explicitly no
    [InlineData(typeof(CustomClassAttribTrue))] // attrib says explicitly true, but not sealed: we might have a sub-class
    [InlineData(typeof(CustomNonBlittableStructNoAttrib))] // no attrib, who knows?
    [InlineData(typeof(CustomNonBlittableStructAttribFalse))] // attrib says explicitly no
    public void MutableTypes(Type type)
    {
        Assert.False((bool)typeof(DefaultHybridCache.ImmutableTypeCache<>).MakeGenericType(type)
            .GetField(nameof(DefaultHybridCache.ImmutableTypeCache<string>.IsImmutable), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!);
    }

    class CustomClassNoAttrib { }
    [ImmutableObject(false)]
    class CustomClassAttribFalse { }
    [ImmutableObject(true)]
    class CustomClassAttribTrue { }
    [ImmutableObject(true)]
    sealed class SealedCustomClassAttribTrue { }

    struct CustomBlittableStruct(int x) { public int X => x; }
    struct CustomNonBlittableStructNoAttrib(string x) { public string X => x; }
    [ImmutableObject(false)]
    struct CustomNonBlittableStructAttribFalse(string x) { public string X => x; }
    [ImmutableObject(true)]
    struct CustomNonBlittableStructAttribTrue(string x) { public string X => x; }
}
