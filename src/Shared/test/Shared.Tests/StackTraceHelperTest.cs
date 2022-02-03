// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.StackTrace.Sources;
using ThrowingLibrary;
using Xunit;

namespace Microsoft.Extensions.Internal;

public class StackTraceHelperTest
{
    [Fact]
    public void StackTraceHelper_IncludesLineNumbersForFiles()
    {
        // Arrange
        Exception exception = null;
        try
        {
            // Throwing an exception in the current assembly always seems to populate the full stack
            // trace regardless of symbol type. Crossing assembly boundaries ensures PortablePdbReader gets used
            // on desktop.
            Thrower.Throw();
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        Assert.Collection(stackFrames,
            frame =>
            {
                Assert.Contains("Thrower.cs", frame.FilePath);
                Assert.Equal(17, frame.LineNumber);
            },
            frame =>
            {
                Assert.Contains("StackTraceHelperTest.cs", frame.FilePath);
            });
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForGenericMethods()
    {
        // Arrange
        var exception = Record.Exception(() => GenericMethod<string>(null));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.GenericMethod<T>(T val)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsWithOutParameters()
    {
        // Arrange
        var exception = Record.Exception(() => MethodWithOutParameter(out var value));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.MethodWithOutParameter(out int value)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsWithGenericOutParameters()
    {
        // Arrange
        var exception = Record.Exception(() => MethodWithGenericOutParameter("Test", out int value));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.MethodWithGenericOutParameter<TVal>(string a, out TVal value)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsWithRefParameters()
    {
        // Arrange
        var value = 0;
        var exception = Record.Exception(() => MethodWithRefParameter(ref value));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.MethodWithRefParameter(ref int value)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsWithGenericRefParameters()
    {
        // Arrange
        var value = 0;
        var exception = Record.Exception(() => MethodWithGenericRefParameter(ref value));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.MethodWithGenericRefParameter<TVal>(ref TVal value)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsWithNullableParameters()
    {
        // Arrange
        var value = 0;
        var exception = Record.Exception(() => MethodWithNullableParameter(value));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.MethodWithNullableParameter(Nullable<int> value)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_PrettyPrintsStackTraceForMethodsOnGenericTypes()
    {
        // Arrange
        var exception = Record.Exception(() => new GenericClass<int>().Throw(0));

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest+GenericClass<T>.Throw(T parameter)", methods[0]);
    }

    [Fact]
    public void StackTraceHelper_ProducesReadableOutput()
    {
        // Arrange
        var expectedCallStack = new List<string>()
            {
                "Microsoft.Extensions.Internal.StackTraceHelperTest.Iterator()+MoveNext()",
                "string.Join(string separator, IEnumerable<string> values)",
                "Microsoft.Extensions.Internal.StackTraceHelperTest+GenericClass<T>.GenericMethod<V>(ref V value)",
                "Microsoft.Extensions.Internal.StackTraceHelperTest.MethodAsync(int value)",
                "Microsoft.Extensions.Internal.StackTraceHelperTest.MethodAsync<TValue>(TValue value)",
                "Microsoft.Extensions.Internal.StackTraceHelperTest.Method(string value)",
                "Microsoft.Extensions.Internal.StackTraceHelperTest.StackTraceHelper_ProducesReadableOutput()",
            };

        Exception exception = null;
        try
        {
            Method("test");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);
        var methodNames = stackFrames.Select(stackFrame => stackFrame.MethodDisplayInfo.ToString()).ToArray();

        // Assert
        Assert.Equal(expectedCallStack, methodNames);
    }

    [Fact]
    public void StackTraceHelper_DoesNotIncludeInstanceMethodsOnTypesWithStackTraceHiddenAttribute()
    {
        // Arrange
        var exception = Record.Exception(() => InvokeMethodOnTypeWithStackTraceHiddenAttribute());

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.ThrowCore()", methods[0]);
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.InvokeMethodOnTypeWithStackTraceHiddenAttribute()", methods[1]);
    }

    [Fact]
    public void StackTraceHelper_DoesNotIncludeStaticMethodsOnTypesWithStackTraceHiddenAttribute()
    {
        // Arrange
        var exception = Record.Exception(() => InvokeStaticMethodOnTypeWithStackTraceHiddenAttribute());

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.ThrowCore()", methods[0]);
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.InvokeStaticMethodOnTypeWithStackTraceHiddenAttribute()", methods[1]);
    }

    [Fact]
    public void StackTraceHelper_DoesNotIncludeMethodsWithStackTraceHiddenAttribute()
    {
        // Arrange
        var exception = Record.Exception(() => new TypeWithMethodWithStackTraceHiddenAttribute().Throw());

        // Act
        var stackFrames = StackTraceHelper.GetFrames(exception, out _);

        // Assert
        var methods = stackFrames.Select(frame => frame.MethodDisplayInfo.ToString()).ToArray();
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest.ThrowCore()", methods[0]);
        Assert.Equal("Microsoft.Extensions.Internal.StackTraceHelperTest+TypeWithMethodWithStackTraceHiddenAttribute.Throw()", methods[1]);
    }

    [Fact]
    public void GetFrames_DoesNotFailForDynamicallyGeneratedAssemblies()
    {
        // Arrange
        var action = (Action)Expression.Lambda(
            Expression.Throw(
                Expression.New(typeof(Exception)))).Compile();
        var exception = Record.Exception(action);

        // Act
        var frames = StackTraceHelper.GetFrames(exception, out _).ToArray();

        // Assert
        var frame = frames[0];
        Assert.Null(frame.FilePath);
        // lambda_method{RandomNumber}(Closure )
        Assert.StartsWith("lambda_method", frame.MethodDisplayInfo.ToString());
        Assert.EndsWith("(Closure )", frame.MethodDisplayInfo.ToString());
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    async Task<string> MethodAsync(int value)
    {
        await Task.Delay(0);
        return GenericClass<byte>.GenericMethod(ref value);
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    async Task<string> MethodAsync<TValue>(TValue value)
    {
        return await MethodAsync(1);
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    string Method(string value)
    {
        return MethodAsync(value).GetAwaiter().GetResult();
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    static IEnumerable<string> Iterator()
    {
        yield return "Success";
        throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void MethodWithOutParameter(out int value) => throw new Exception();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void MethodWithGenericOutParameter<TVal>(string a, out TVal value) => throw new Exception();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void MethodWithRefParameter(ref int value) => throw new Exception();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void MethodWithGenericRefParameter<TVal>(ref TVal value) => throw new Exception();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void MethodWithNullableParameter(int? value) => throw new Exception();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void InvokeMethodOnTypeWithStackTraceHiddenAttribute() => new TypeWithStackTraceHiddenAttribute().Throw();

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    void InvokeStaticMethodOnTypeWithStackTraceHiddenAttribute() => TypeWithStackTraceHiddenAttribute.ThrowStatic();

    class GenericClass<T>
    {
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static string GenericMethod<V>(ref V value)
        {
            var returnVal = "";
            for (var i = 0; i < 10; i++)
            {
                returnVal += string.Join(", ", Iterator());
            }
            return returnVal;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void Throw(T parameter) => throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private void GenericMethod<T>(T val) where T : class => throw new Exception();

    private class StackTraceHiddenAttribute : Attribute
    {
    }

    [StackTraceHidden]
    private class TypeWithStackTraceHiddenAttribute
    {
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void Throw() => ThrowCore();

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public static void ThrowStatic() => ThrowCore();
    }

    private class TypeWithMethodWithStackTraceHiddenAttribute
    {
        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        [StackTraceHidden]
        public void MethodWithStackTraceHiddenAttribute()
        {
            ThrowCore();
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public void Throw() => MethodWithStackTraceHiddenAttribute();
    }

    [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
    private static void ThrowCore() => throw new Exception();
}
