// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class TypeNameHashTest
{
    // In these tests, we're mostly interested in checking that the hash function succeeds
    // for any type with a valid name. We'll also do some basic sanity checking by ensuring
    // that the string representation of the hash has the expected length.

    // We currently use a hex-encoded SHA256 hash, so there should be two characters per byte
    // of encoded data.
    private const int ExpectedHashLength = SHA256.HashSizeInBytes * 2;

    [Fact]
    public void CanComputeHashForTypeWithBasicName()
    {
        // Act
        var hash = TypeNameHash.Compute(typeof(ClassWithBasicName));

        // Assert
        Assert.Equal(ExpectedHashLength, hash.Length);
    }

    [Fact]
    public void CanComputeHashForTypeWithMultibyteCharacters()
    {
        // Act
        var hash = TypeNameHash.Compute(typeof(ClássWïthMûltibyteÇharacters));

        // Assert
        Assert.Equal(ExpectedHashLength, hash.Length);
    }

    [Fact]
    public void CanComputeHashForAnonymousType()
    {
        // Arrange
        var type = new { Foo = "bar" }.GetType();

        // Act
        var hash = TypeNameHash.Compute(type);

        // Assert
        Assert.Equal(ExpectedHashLength, hash.Length);
    }

    [Fact]
    public void CanComputeHashForTypeWithNameLongerThanMaxStackBufferSize()
    {
        // Arrange
        // We need to use a type with a long name, so we'll use a large tuple.
        // We have an assert later in this test to sanity check that the type
        // name is indeed longer than the max stack buffer size.
        var type = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12).GetType();

        // Act
        var hash = TypeNameHash.Compute(type);

        // Assert
        Assert.True(type.FullName.Length > TypeNameHash.MaxStackBufferSize);
        Assert.Equal(ExpectedHashLength, hash.Length);
    }

    [Fact]
    public void ThrowsIfTypeHasNoName()
    {
        // Arrange
        var type = typeof(Nullable<>).GetGenericArguments()[0];

        // Act/Assert
        var ex = Assert.Throws<InvalidOperationException>(() => TypeNameHash.Compute(type));
        Assert.Equal($"Cannot compute a hash for a type without a {nameof(Type.FullName)}.", ex.Message);
    }

    class ClassWithBasicName;
    class ClássWïthMûltibyteÇharacters;
}
