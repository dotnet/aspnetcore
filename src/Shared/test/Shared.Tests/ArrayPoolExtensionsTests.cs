// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Buffers;

public sealed class ArrayPoolExtensionsTests
{
    private record struct StructWithStringField(string Value);

    [Fact]
    public void Return_PartiallyClearsArray_UnmanagedType_WithPartialLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.Return(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept(0) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(int.MaxValue) < 0);
    }

    [Fact]
    public void Return_PartiallyClearsArray_ManagedValueType_WithPartialLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.Return(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept(default(StructWithStringField)) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(value) < 0);
    }

    [Fact]
    public void Return_PartiallyClearsArray_ReferenceType_WithPartialLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.Return(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept((string)null) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(Value) < 0);
    }

    [Fact]
    public void Return_CompletelyClearsArray_UnmanagedType_WithFullLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.Return(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept(0) < 0);
    }

    [Fact]
    public void Return_CompletelyClearsArray_ManagedValueType_WithFullLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.Return(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept(default(StructWithStringField)) < 0);
    }

    [Fact]
    public void Return_CompletelyClearsArray_ReferenceType_WithFullLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.Return(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept((string)null) < 0);
    }

    [Fact]
    public void Return_DoesNotClearArray_UnmanagedType_WithZeroLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.Return(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(int.MaxValue) < 0);
    }

    [Fact]
    public void Return_DoesNotClearArray_ManagedValueType_WithZeroLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.Return(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(value) < 0);
    }

    [Fact]
    public void Return_DoesNotClearArray_ReferenceType_WithZeroLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.Return(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(Value) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_PartiallyClearsArray_ReferenceType_WithPartialLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.ReturnAndClearReferences(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept((string)null) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(Value) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_PartiallyClearsArray_ManagedValueType_WithPartialLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.ReturnAndClearReferences(array, 42);

        Assert.True(array.AsSpan(0, 42).IndexOfAnyExcept(default(StructWithStringField)) < 0);
        Assert.True(array.AsSpan(42).IndexOfAnyExcept(value) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_DoesNotClearArray_UnmanagedType_WithPartialLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.ReturnAndClearReferences(array, 42);

        Assert.True(array.AsSpan().IndexOfAnyExcept(int.MaxValue) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_CompletelyClearsArray_ReferenceType_WithFullLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.ReturnAndClearReferences(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept((string)null) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_CompletelyClearsArray_ManagedValueType_WithFullLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.ReturnAndClearReferences(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept(default(StructWithStringField)) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_DoesNotClearArray_UnmanagedType_WithFullLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.ReturnAndClearReferences(array, array.Length);

        Assert.True(array.AsSpan().IndexOfAnyExcept(int.MaxValue) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_DoesNotClearArray_ReferenceType_WithZeroLengthSpecified()
    {
        const string Value = "abc";

        ArrayPool<string> pool = ArrayPool<string>.Create();
        string[] array = pool.Rent(64);

        array.AsSpan().Fill(Value);

        pool.ReturnAndClearReferences(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(Value) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_DoesNotClearArray_ManagedValueType_WithZeroLengthSpecified()
    {
        ArrayPool<StructWithStringField> pool = ArrayPool<StructWithStringField>.Create();
        StructWithStringField[] array = pool.Rent(64);

        StructWithStringField value = new("abc");
        array.AsSpan().Fill(value);

        pool.ReturnAndClearReferences(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(value) < 0);
    }

    [Fact]
    public void ReturnAndClearReferences_DoesNotClearArray_UnmanagedType_WithZeroLengthSpecified()
    {
        ArrayPool<int> pool = ArrayPool<int>.Create();
        int[] array = pool.Rent(64);

        array.AsSpan().Fill(int.MaxValue);

        pool.ReturnAndClearReferences(array, 0);

        Assert.True(array.AsSpan().IndexOfAnyExcept(int.MaxValue) < 0);
    }
}
