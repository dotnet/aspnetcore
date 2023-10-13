// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Http.Extensions.Tests;

public class ParameterBindingMethodCacheTests
{
    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(Half))]
    [InlineData(typeof(short))]
    [InlineData(typeof(long))]
    [InlineData(typeof(IntPtr))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    public void FindTryParseStringMethod_ReturnsTheExpectedTryParseMethodWithInvariantCulture(Type type)
    {
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(@type);

        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(type, "parsedValue"), Expression.Constant(CultureInfo.InvariantCulture)) as MethodCallExpression;
        Assert.NotNull(call);
        var parameters = call!.Method.GetParameters();

        Assert.Equal(4, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(NumberStyles), parameters[1].ParameterType);
        Assert.Equal(typeof(IFormatProvider), parameters[2].ParameterType);
        Assert.True(parameters[3].IsOut);
    }

    [Fact]
    public void FindUriTryCreateStringMethod_ReturnsTheExpectedUriTryCreateMethod()
    {
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(typeof(Uri));

        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(typeof(Uri), "parsedValue"), Expression.Constant(UriKind.RelativeOrAbsolute)) as MethodCallExpression;
        Assert.NotNull(call);
        var parameters = call!.Method.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(UriKind), parameters[1].ParameterType);
        Assert.True(parameters[2].IsOut);
    }

    [Theory]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(DateOnly))]
    [InlineData(typeof(DateTimeOffset))]
    [InlineData(typeof(TimeOnly))]
    [InlineData(typeof(TimeSpan))]
    public void FindTryParseStringMethod_ReturnsTheExpectedTryParseMethodWithInvariantCultureDateType(Type type)
    {
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(@type);

        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(type, "parsedValue"), Expression.Constant(CultureInfo.InvariantCulture)) as MethodCallExpression;
        Assert.NotNull(call);
        var parameters = call!.Method.GetParameters();

        if (@type == typeof(TimeSpan))
        {
            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(IFormatProvider), parameters[1].ParameterType);
            Assert.True(parameters[2].IsOut);
        }
        else
        {
            Assert.Equal(4, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(IFormatProvider), parameters[1].ParameterType);
            Assert.Equal(typeof(DateTimeStyles), parameters[2].ParameterType);
            Assert.True(parameters[3].IsOut);
        }
    }

    [Theory]
    [InlineData(typeof(TryParseStringRecord))]
    [InlineData(typeof(TryParseStringStruct))]
    [InlineData(typeof(TryParseInheritClassWithFormatProvider))]
    [InlineData(typeof(TryParseFromInterfaceWithFormatProvider))]
    public void FindTryParseStringMethod_ReturnsTheExpectedTryParseMethodWithInvariantCultureCustomType(Type type)
    {
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(@type);

        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(type, "parsedValue"), Expression.Constant(CultureInfo.InvariantCulture)) as MethodCallExpression;
        Assert.NotNull(call);
        var parameters = call!.Method.GetParameters();

        Assert.Equal(3, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(IFormatProvider), parameters[1].ParameterType);
        Assert.True(parameters[2].IsOut);
        Assert.True(((call.Arguments[1] as ConstantExpression)!.Value as CultureInfo)!.Equals(CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData(typeof(TryParseNoFormatProviderRecord))]
    [InlineData(typeof(TryParseNoFormatProviderStruct))]
    [InlineData(typeof(TryParseInheritClass))]
    [InlineData(typeof(TryParseFromInterface))]
    [InlineData(typeof(TryParseFromGrandparentInterface))]
    [InlineData(typeof(TryParseDirectlyAndFromInterface))]
    [InlineData(typeof(TryParseFromClassAndInterface))]
    public void FindTryParseMethod_WithNoFormatProvider(Type type)
    {
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(@type);
        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(type, "parsedValue"), Expression.Constant(CultureInfo.InvariantCulture)) as MethodCallExpression;
        Assert.NotNull(call);
        var parameters = call!.Method.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.True(parameters[1].IsOut);
    }

    public static IEnumerable<object[]> TryParseStringParameterInfoData
    {
        get
        {
            return new[]
            {
                    new[]
                    {
                        GetFirstParameter((TryParseStringRecord arg) => TryParseStringRecordMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((TryParseStringStruct arg) => TryParseStringStructMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((TryParseStringStruct? arg) => TryParseStringNullableStructMethod(arg)),
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(TryParseStringParameterInfoData))]
    public void HasTryParseStringMethod_ReturnsTrueWhenMethodExists(ParameterInfo parameterInfo)
    {
        Assert.True(new ParameterBindingMethodCache().HasTryParseMethod(parameterInfo.ParameterType));
    }

    [Fact]
    public void FindTryParseStringMethod_FindsExplicitlyImplementedIParsable()
    {
        var type = typeof(TodoWithExplicitIParsable);
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(type);
        Assert.NotNull(methodFound);
    }

    [Fact]
    public void FindTryParseStringMethod_WorksForEnums()
    {
        var type = typeof(Choice);
        var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(type);

        Assert.NotNull(methodFound);

        var call = methodFound!(Expression.Variable(type, "parsedValue"), Expression.Constant(CultureInfo.InvariantCulture)) as MethodCallExpression;
        Assert.NotNull(call);
        var method = call!.Method;
        var parameters = method.GetParameters();

        // By default, we use Enum.TryParse<T>
        Assert.True(method.IsGenericMethod);
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.True(parameters[1].IsOut);
    }

    [Fact]
    public void FindTryParseStringMethod_WorksForEnumsWhenNonGenericEnumParseIsUsed()
    {
        var type = typeof(Choice);
        var cache = new ParameterBindingMethodCache(preferNonGenericEnumParseOverload: true);
        var methodFound = cache.FindTryParseMethod(type);

        Assert.NotNull(methodFound);

        var parsedValue = Expression.Variable(type, "parsedValue");
        var block = methodFound!(parsedValue, Expression.Constant(CultureInfo.InvariantCulture)) as BlockExpression;
        Assert.NotNull(block);
        Assert.Equal(typeof(bool), block!.Type);

        var parseEnum = Expression.Lambda<Func<string, Choice>>(Expression.Block(new[] { parsedValue },
            block,
            parsedValue), ParameterBindingMethodCache.SharedExpressions.TempSourceStringExpr).Compile();

        Assert.Equal(Choice.One, parseEnum("One"));
        Assert.Equal(Choice.Two, parseEnum("Two"));
        Assert.Equal(Choice.Three, parseEnum("Three"));
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsCorrectMethodOnClass()
    {
        var type = typeof(BindAsyncRecord);
        var cache = new ParameterBindingMethodCache();
        var parameter = new MockParameterInfo(type, "bindAsyncRecord");
        var methodFound = cache.FindBindAsyncMethod(parameter);

        Assert.NotNull(methodFound.Expression);
        Assert.Equal(2, methodFound.ParamCount);

        var parsedValue = Expression.Variable(type, "parsedValue");

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(
            Expression.Block(new[] { parsedValue }, methodFound.Expression!),
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext
        {
            Request =
                {
                    Headers =
                    {
                        ["ETag"] = "42",
                    },
                },
        };

        Assert.Equal(new BindAsyncRecord(42), await parseHttpContext(httpContext));
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsSingleArgBindAsync()
    {
        var type = typeof(BindAsyncSingleArgStruct);
        var cache = new ParameterBindingMethodCache();
        var parameter = new MockParameterInfo(type, "bindAsyncSingleArgStruct");
        var methodFound = cache.FindBindAsyncMethod(parameter);

        Assert.NotNull(methodFound.Expression);
        Assert.Equal(1, methodFound.ParamCount);

        var parsedValue = Expression.Variable(type, "parsedValue");

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(
            Expression.Block(new[] { parsedValue }, methodFound.Expression!),
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext
        {
            Request =
                {
                    Headers =
                    {
                        ["ETag"] = "42",
                    },
                },
        };

        Assert.Equal(new BindAsyncSingleArgStruct(42), await parseHttpContext(httpContext));
    }

    public static IEnumerable<object[]> BindAsyncParameterInfoData
    {
        get
        {
            return new[]
            {
                    new[]
                    {
                        GetFirstParameter((BindAsyncRecord arg) => BindAsyncRecordMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncStruct arg) => BindAsyncStructMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncSingleArgRecord arg) => BindAsyncSingleArgRecordMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncSingleArgStruct arg) => BindAsyncSingleArgStructMethod(arg)),
                    },
                    new[]
                    {
                        GetFirstParameter((InheritBindAsync arg) => InheritBindAsyncMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((InheritBindAsyncWithParameterInfo arg) => InheritBindAsyncWithParameterInfoMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncFromInterface arg) => BindAsyncFromInterfaceMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncFromGrandparentInterface arg) => BindAsyncFromGrandparentInterfaceMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncDirectlyAndFromInterface arg) => BindAsyncDirectlyAndFromInterfaceMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncFromClassAndInterface arg) => BindAsyncFromClassAndInterfaceMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncFromInterfaceWithParameterInfo arg) => BindAsyncFromInterfaceWithParameterInfoMethod(arg))
                    },
                    new[]
                    {
                        GetFirstParameter((BindAsyncFromStaticAbstractInterfaceAndBindAsync arg) => BindAsyncFromImplicitStaticAbstractInterfaceMethodInsteadOfReflectionMatchedMethod(arg))
                    },
                };
        }
    }

    [Theory]
    [MemberData(nameof(BindAsyncParameterInfoData))]
    public void HasBindAsyncMethod_ReturnsTrueWhenMethodExists(ParameterInfo parameterInfo)
    {
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public void HasBindAsyncMethod_ReturnsTrueForNullableReturningBindAsyncStructMethod()
    {
        var parameterInfo = GetFirstParameter((NullableReturningBindAsyncStruct arg) => NullableReturningBindAsyncStructMethod(arg));
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public void HasBindAsyncMethod_ReturnsTrueForClassImplicitlyImplementingIBindableFromHttpContext()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromImplicitStaticAbstractInterface arg) => BindAsyncFromImplicitStaticAbstractInterfaceMethod(arg));
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public void HasBindAsyncMethod_ReturnsTrueForClassExplicitlyImplementingIBindableFromHttpContext()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromExplicitStaticAbstractInterface arg) => BindAsyncFromExplicitStaticAbstractInterfaceMethod(arg));
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public void HasBindAsyncMethod_ReturnsTrueForClassImplementingIBindableFromHttpContextAndNonInterfaceBindAsyncMethod()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromStaticAbstractInterfaceAndBindAsync arg) => BindAsyncFromImplicitStaticAbstractInterfaceMethodInsteadOfReflectionMatchedMethod(arg));
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public void FindBindAsyncMethod_FindsNonNullableReturningBindAsyncMethodGivenNullableType()
    {
        var parameterInfo = GetFirstParameter((BindAsyncStruct? arg) => BindAsyncNullableStructMethod(arg));
        Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsForClassImplicitlyImplementingIBindableFromHttpContext()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromImplicitStaticAbstractInterface arg) => BindAsyncFromImplicitStaticAbstractInterfaceMethod(arg));
        var cache = new ParameterBindingMethodCache();
        Assert.True(cache.HasBindAsyncMethod(parameterInfo));
        var methodFound = cache.FindBindAsyncMethod(parameterInfo);

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object?>>>(methodFound.Expression!,
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext();

        var result = await parseHttpContext(httpContext);
        Assert.NotNull(result);
        Assert.IsType<BindAsyncFromImplicitStaticAbstractInterface>(result);
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsForClassExplicitlyImplementingIBindableFromHttpContext()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromExplicitStaticAbstractInterface arg) => BindAsyncFromExplicitStaticAbstractInterfaceMethod(arg));
        var cache = new ParameterBindingMethodCache();
        Assert.True(cache.HasBindAsyncMethod(parameterInfo));
        var methodFound = cache.FindBindAsyncMethod(parameterInfo);

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object?>>>(methodFound.Expression!,
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext();

        var result = await parseHttpContext(httpContext);
        Assert.NotNull(result);
        Assert.IsType<BindAsyncFromExplicitStaticAbstractInterface>(result);
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsFallbackMethodWhenPreferredMethodsReturnTypeIsWrong()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFallsBack? arg) => BindAsyncFallbackMethod(arg));
        var cache = new ParameterBindingMethodCache();
        Assert.True(cache.HasBindAsyncMethod(parameterInfo));
        var methodFound = cache.FindBindAsyncMethod(parameterInfo);

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(methodFound.Expression!,
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext();

        Assert.Null(await parseHttpContext(httpContext));
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsFallbackMethodFromInheritedWhenPreferredMethodIsInvalid()
    {
        var parameterInfo = GetFirstParameter((BindAsyncBadMethod? arg) => BindAsyncBadMethodMethod(arg));
        var cache = new ParameterBindingMethodCache();
        Assert.True(cache.HasBindAsyncMethod(parameterInfo));
        var methodFound = cache.FindBindAsyncMethod(parameterInfo);

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(methodFound.Expression!,
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext();

        Assert.Null(await parseHttpContext(httpContext));
    }

    [Fact]
    public async Task FindBindAsyncMethod_FindsMethodFromStaticAbstractInterfaceWhenValidNonInterfaceMethodAlsoExists()
    {
        var parameterInfo = GetFirstParameter((BindAsyncFromStaticAbstractInterfaceAndBindAsync arg) => BindAsyncFromImplicitStaticAbstractInterfaceMethodInsteadOfReflectionMatchedMethod(arg));
        var cache = new ParameterBindingMethodCache();
        Assert.True(cache.HasBindAsyncMethod(parameterInfo));
        var methodFound = cache.FindBindAsyncMethod(parameterInfo);

        var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(methodFound.Expression!,
            ParameterBindingMethodCache.SharedExpressions.HttpContextExpr).Compile();

        var httpContext = new DefaultHttpContext();
        var result = await parseHttpContext(httpContext);

        Assert.NotNull(result);
        Assert.IsType<BindAsyncFromStaticAbstractInterfaceAndBindAsync>(result);
        Assert.Equal(BindAsyncSource.InterfaceStaticAbstractImplicit, ((BindAsyncFromStaticAbstractInterfaceAndBindAsync)result).BoundFrom);
    }

    [Theory]
    [InlineData(typeof(ClassWithParameterlessConstructor))]
    [InlineData(typeof(RecordClassParameterlessConstructor))]
    [InlineData(typeof(StructWithParameterlessConstructor))]
    [InlineData(typeof(RecordStructWithParameterlessConstructor))]
    public void FindConstructor_FindsParameterlessConstructor_WhenExplicitlyDeclared(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var (constructor, parameters) = cache.FindConstructor(type);

        Assert.NotNull(constructor);
        Assert.True(parameters.Length == 0);
    }

    [Theory]
    [InlineData(typeof(ClassWithDefaultConstructor))]
    [InlineData(typeof(RecordClassWithDefaultConstructor))]
    public void FindConstructor_FindsDefaultConstructor_WhenNotExplictlyDeclared(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var (constructor, parameters) = cache.FindConstructor(type);

        Assert.NotNull(constructor);
        Assert.True(parameters.Length == 0);
    }

    [Theory]
    [InlineData(typeof(ClassWithParameterizedConstructor))]
    [InlineData(typeof(RecordClassParameterizedConstructor))]
    [InlineData(typeof(StructWithParameterizedConstructor))]
    [InlineData(typeof(RecordStructParameterizedConstructor))]
    public void FindConstructor_FindsParameterizedConstructor_WhenExplictlyDeclared(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var (constructor, parameters) = cache.FindConstructor(type);

        Assert.NotNull(constructor);
        Assert.True(parameters.Length == 1);
    }

    [Theory]
    [InlineData(typeof(StructWithDefaultConstructor))]
    [InlineData(typeof(RecordStructWithDefaultConstructor))]
    public void FindConstructor_ReturnNullForStruct_WhenNotExplictlyDeclared(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var (constructor, parameters) = cache.FindConstructor(type);

        Assert.Null(constructor);
        Assert.True(parameters.Length == 0);
    }

    [Theory]
    [InlineData(typeof(StructWithMultipleConstructors))]
    [InlineData(typeof(RecordStructWithMultipleConstructors))]
    public void FindConstructor_ReturnNullForStruct_WhenMultipleParameterizedConstructorsDeclared(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var (constructor, parameters) = cache.FindConstructor(type);

        Assert.Null(constructor);
        Assert.True(parameters.Length == 0);
    }

    public static TheoryData<Type> InvalidTryParseStringTypesData
    {
        get
        {
            return new TheoryData<Type>
            {
                typeof(InvalidVoidReturnTryParseStruct),
                typeof(InvalidVoidReturnTryParseClass),
                typeof(InvalidWrongTypeTryParseStruct),
                typeof(InvalidWrongTypeTryParseClass),
                typeof(InvalidTryParseNullableStruct),
                typeof(InvalidTooFewArgsTryParseStruct),
                typeof(InvalidTooFewArgsTryParseClass),
                typeof(InvalidNonStaticTryParseStruct),
                typeof(InvalidNonStaticTryParseClass),
                typeof(TryParseWrongTypeInheritClass),
                typeof(TryParseWrongTypeFromInterface),
            };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTryParseStringTypesData))]
    public void FindTryParseMethod_ThrowsIfInvalidTryParseOnType(Type type)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ParameterBindingMethodCache().FindTryParseMethod(type));
        Assert.StartsWith($"TryParse method found on {TypeNameHelper.GetTypeDisplayName(type, fullName: false)} with incorrect format. Must be a static method with format", ex.Message);
        Assert.Contains($"bool TryParse(string, IFormatProvider, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})", ex.Message);
        Assert.Contains($"bool TryParse(string, out {TypeNameHelper.GetTypeDisplayName(type, fullName: false)})", ex.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidTryParseStringTypesData))]
    public void FindTryParseMethod_DoesNotThrowIfInvalidTryParseOnType_WhenThrowOnInvalidFalse(Type type)
    {
        Assert.Null(new ParameterBindingMethodCache(throwOnInvalidMethod: false).FindTryParseMethod(type));
    }

    [Fact]
    public void FindTryParseMethod_ThrowsIfMultipleInterfacesMatch()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => new ParameterBindingMethodCache().FindTryParseMethod(typeof(TryParseFromMultipleInterfaces)));
        Assert.Equal("TryParseFromMultipleInterfaces implements multiple interfaces defining a static Boolean TryParse(System.String, TryParseFromMultipleInterfaces ByRef) method causing ambiguity.", ex.Message);
    }

    [Fact]
    public void FindTryParseMethod_DoesNotThrowIfMultipleInterfacesMatch_WhenThrowOnInvalidFalse()
    {
        Assert.Null(new ParameterBindingMethodCache(throwOnInvalidMethod: false).FindTryParseMethod(typeof(TryParseFromMultipleInterfaces)));
    }

    [Theory]
    [InlineData(typeof(TryParseClassWithGoodAndBad))]
    [InlineData(typeof(TryParseStructWithGoodAndBad))]
    public void FindTryParseMethod_IgnoresInvalidTryParseIfGoodOneFound(Type type)
    {
        var method = new ParameterBindingMethodCache().FindTryParseMethod(type);
        Assert.NotNull(method);
    }

    public static TheoryData<Type> InvalidBindAsyncTypesData
    {
        get
        {
            return new TheoryData<Type>
            {
                typeof(InvalidWrongReturnBindAsyncStruct),
                typeof(InvalidWrongReturnBindAsyncClass),
                typeof(InvalidWrongParamBindAsyncStruct),
                typeof(InvalidWrongParamBindAsyncClass),
                typeof(BindAsyncWrongTypeInherit),
                typeof(BindAsyncWithParameterInfoWrongTypeInherit),
                typeof(BindAsyncWrongTypeFromInterface),
                typeof(BindAsyncBothBadMethods),
                typeof(BindAsyncFromStaticAbstractInterfaceWrongType)
            };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidBindAsyncTypesData))]
    public void FindBindAsyncMethod_ThrowsIfInvalidBindAsyncOnType(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var parameter = new MockParameterInfo(type, "anything");
        var ex = Assert.Throws<InvalidOperationException>(
            () => cache.FindBindAsyncMethod(parameter));
        Assert.StartsWith($"BindAsync method found on {TypeNameHelper.GetTypeDisplayName(type, fullName: false)} with incorrect format. Must be a static method with format", ex.Message);
        Assert.Contains($"ValueTask<{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}> BindAsync(HttpContext context, ParameterInfo parameter)", ex.Message);
        Assert.Contains($"ValueTask<{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}> BindAsync(HttpContext context)", ex.Message);
        Assert.Contains($"ValueTask<{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}?> BindAsync(HttpContext context, ParameterInfo parameter)", ex.Message);
        Assert.Contains($"ValueTask<{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}?> BindAsync(HttpContext context)", ex.Message);
    }

    [Theory]
    [MemberData(nameof(InvalidBindAsyncTypesData))]
    public void FindBindAsyncMethod_DoesNotThrowIfInvalidBindAsyncOnType_WhenThrowOnInvalidFalse(Type type)
    {
        var cache = new ParameterBindingMethodCache(throwOnInvalidMethod: false);
        var parameter = new MockParameterInfo(type, "anything");
        var (expression, _) = cache.FindBindAsyncMethod(parameter);
        Assert.Null(expression);
    }

    [Fact]
    public void FindBindAsyncMethod_ThrowsIfMultipleInterfacesMatch()
    {
        var cache = new ParameterBindingMethodCache();
        var parameter = new MockParameterInfo(typeof(BindAsyncFromMultipleInterfaces), "anything");
        var ex = Assert.Throws<InvalidOperationException>(() => cache.FindBindAsyncMethod(parameter));
        Assert.Equal("BindAsyncFromMultipleInterfaces implements multiple interfaces defining a static System.Threading.Tasks.ValueTask`1[Microsoft.AspNetCore.Http.Extensions.Tests.ParameterBindingMethodCacheTests+BindAsyncFromMultipleInterfaces] BindAsync(Microsoft.AspNetCore.Http.HttpContext) method causing ambiguity.", ex.Message);
    }

    [Fact]
    public void FindBindAsyncMethod_DoesNotThrowIfMultipleInterfacesMatch_WhenThrowOnInvalidFalse()
    {
        var cache = new ParameterBindingMethodCache(throwOnInvalidMethod: false);
        var parameter = new MockParameterInfo(typeof(BindAsyncFromMultipleInterfaces), "anything");
        var (expression, _) = cache.FindBindAsyncMethod(parameter);
        Assert.Null(expression);
    }

    [Theory]
    [InlineData(typeof(BindAsyncStructWithGoodAndBad))]
    [InlineData(typeof(BindAsyncClassWithGoodAndBad))]
    public void FindBindAsyncMethod_IgnoresInvalidBindAsyncIfGoodOneFound(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var parameter = new MockParameterInfo(type, "anything");
        var (expression, _) = cache.FindBindAsyncMethod(parameter);
        Assert.NotNull(expression);
    }

    private class ClassWithInternalConstructor
    {
        internal ClassWithInternalConstructor()
        { }
    }
    private record RecordWithInternalConstructor
    {
        internal RecordWithInternalConstructor()
        { }
    }

    [Theory]
    [InlineData(typeof(ClassWithInternalConstructor))]
    [InlineData(typeof(RecordWithInternalConstructor))]
    public void FindConstructor_ThrowsIfNoPublicConstructors(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var ex = Assert.Throws<InvalidOperationException>(() => cache.FindConstructor(type));
        Assert.Equal($"No public parameterless constructor found for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.", ex.Message);
    }

    [Theory]
    [InlineData(typeof(AbstractClass))]
    [InlineData(typeof(AbstractRecord))]
    public void FindConstructor_ThrowsIfAbstract(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var ex = Assert.Throws<InvalidOperationException>(() => cache.FindConstructor(type));
        Assert.Equal($"The abstract type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}' is not supported.", ex.Message);
    }

    [Theory]
    [InlineData(typeof(ClassWithMultipleConstructors))]
    [InlineData(typeof(RecordWithMultipleConstructors))]
    public void FindConstructor_ThrowsIfMultipleParameterizedConstructors(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var ex = Assert.Throws<InvalidOperationException>(() => cache.FindConstructor(type));
        Assert.Equal($"Only a single public parameterized constructor is allowed for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.", ex.Message);
    }

    [Theory]
    [InlineData(typeof(ClassWithInvalidConstructors))]
    [InlineData(typeof(RecordClassWithInvalidConstructors))]
    [InlineData(typeof(RecordStructWithInvalidConstructors))]
    [InlineData(typeof(StructWithInvalidConstructors))]
    public void FindConstructor_ThrowsIfParameterizedConstructorIncludeNoMatchingArguments(Type type)
    {
        var cache = new ParameterBindingMethodCache();
        var ex = Assert.Throws<InvalidOperationException>(() => cache.FindConstructor(type));
        Assert.Equal(
            $"The public parameterized constructor must contain only parameters that match the declared public properties for type '{TypeNameHelper.GetTypeDisplayName(type, fullName: false)}'.",
            ex.Message);
    }

    enum Choice
    {
        One,
        Two,
        Three
    }

    private static void TryParseStringRecordMethod(TryParseStringRecord arg) { }
    private static void TryParseStringStructMethod(TryParseStringStruct arg) { }
    private static void TryParseStringNullableStructMethod(TryParseStringStruct? arg) { }

    private static void BindAsyncRecordMethod(BindAsyncRecord arg) { }
    private static void BindAsyncStructMethod(BindAsyncStruct arg) { }
    private static void BindAsyncNullableStructMethod(BindAsyncStruct? arg) { }
    private static void NullableReturningBindAsyncStructMethod(NullableReturningBindAsyncStruct arg) { }
    private static void BindAsyncSingleArgRecordMethod(BindAsyncSingleArgRecord arg) { }
    private static void BindAsyncSingleArgStructMethod(BindAsyncSingleArgStruct arg) { }
    private static void InheritBindAsyncMethod(InheritBindAsync arg) { }
    private static void InheritBindAsyncWithParameterInfoMethod(InheritBindAsyncWithParameterInfo args) { }
    private static void BindAsyncFromInterfaceMethod(BindAsyncFromInterface arg) { }
    private static void BindAsyncFromGrandparentInterfaceMethod(BindAsyncFromGrandparentInterface arg) { }
    private static void BindAsyncDirectlyAndFromInterfaceMethod(BindAsyncDirectlyAndFromInterface arg) { }
    private static void BindAsyncFromClassAndInterfaceMethod(BindAsyncFromClassAndInterface arg) { }
    private static void BindAsyncFromInterfaceWithParameterInfoMethod(BindAsyncFromInterfaceWithParameterInfo args) { }
    private static void BindAsyncFallbackMethod(BindAsyncFallsBack? arg) { }
    private static void BindAsyncBadMethodMethod(BindAsyncBadMethod? arg) { }
    private static void BindAsyncFromImplicitStaticAbstractInterfaceMethod(BindAsyncFromImplicitStaticAbstractInterface arg) { }
    private static void BindAsyncFromExplicitStaticAbstractInterfaceMethod(BindAsyncFromExplicitStaticAbstractInterface arg) { }
    private static void BindAsyncFromImplicitStaticAbstractInterfaceMethodInsteadOfReflectionMatchedMethod(BindAsyncFromStaticAbstractInterfaceAndBindAsync arg) { }
    private static void BindAsyncFromStaticAbstractInterfaceWrongTypeMethod(BindAsyncFromStaticAbstractInterfaceWrongType arg) { }

    private static ParameterInfo GetFirstParameter<T>(Expression<Action<T>> expr)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method.GetParameters()[0];
    }

    private static ParameterInfo GetParameterAtIndex<T>(Expression<Action<T>> expr, int paramIndex)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method.GetParameters()[paramIndex];
    }

    private record TryParseStringRecord(int Value)
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out TryParseStringRecord? result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = null;
                return false;
            }

            result = new TryParseStringRecord(val);
            return true;
        }
    }

    private record struct TryParseStringStruct(int Value)
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out TryParseStringStruct result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = default;
                return false;
            }

            result = new TryParseStringStruct(val);
            return true;
        }
    }

    private record struct InvalidVoidReturnTryParseStruct(int Value)
    {
        public static void TryParse(string? value, IFormatProvider formatProvider, out InvalidVoidReturnTryParseStruct result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = default;
                return;
            }

            result = new InvalidVoidReturnTryParseStruct(val);
            return;
        }
    }

    private record struct InvalidWrongTypeTryParseStruct(int Value)
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out InvalidVoidReturnTryParseStruct result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = default;
                return false;
            }

            result = new InvalidVoidReturnTryParseStruct(val);
            return true;
        }
    }

    private record struct InvalidTryParseNullableStruct(int Value)
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out InvalidTryParseNullableStruct? result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = default;
                return false;
            }

            result = new InvalidTryParseNullableStruct(val);
            return true;
        }
    }

    private record struct InvalidTooFewArgsTryParseStruct(int Value)
    {
        public static bool TryParse(out InvalidTooFewArgsTryParseStruct result)
        {
            result = default;
            return false;
        }
    }

    private struct TryParseStructWithGoodAndBad
    {
        public static bool TryParse(string? value, out TryParseStructWithGoodAndBad result)
        {
            result = new();
            return false;
        }

        public static void TryParse(out TryParseStructWithGoodAndBad result)
        {
            result = new();
        }
    }

    private record struct InvalidNonStaticTryParseStruct(int Value)
    {
        public bool TryParse(string? value, IFormatProvider formatProvider, out InvalidVoidReturnTryParseStruct result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = default;
                return false;
            }

            result = new InvalidVoidReturnTryParseStruct(val);
            return true;
        }
    }

    private class InvalidVoidReturnTryParseClass
    {
        public static void TryParse(string? value, IFormatProvider formatProvider, out InvalidVoidReturnTryParseClass result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = new();
                return;
            }

            result = new();
        }
    }

    private class InvalidWrongTypeTryParseClass
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out InvalidVoidReturnTryParseClass result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = new();
                return false;
            }

            result = new();
            return true;
        }
    }

    private class InvalidTooFewArgsTryParseClass
    {
        public static bool TryParse(out InvalidTooFewArgsTryParseClass result)
        {
            result = new();
            return false;
        }
    }

    private class TryParseClassWithGoodAndBad
    {
        public static bool TryParse(string? value, out TryParseClassWithGoodAndBad result)
        {
            result = new();
            return false;
        }

        public static bool TryParse(out TryParseClassWithGoodAndBad result)
        {
            result = new();
            return false;
        }
    }

    private class InvalidNonStaticTryParseClass
    {
        public bool TryParse(string? value, IFormatProvider formatProvider, out InvalidNonStaticTryParseClass result)
        {
            if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
            {
                result = new();
                return false;
            }

            result = new();
            return true;
        }
    }

    private record TryParseNoFormatProviderRecord(int Value)
    {
        public static bool TryParse(string? value, out TryParseNoFormatProviderRecord? result)
        {
            if (!int.TryParse(value, out var val))
            {
                result = null;
                return false;
            }

            result = new TryParseNoFormatProviderRecord(val);
            return true;
        }
    }

    private record struct TryParseNoFormatProviderStruct(int Value)
    {
        public static bool TryParse(string? value, out TryParseNoFormatProviderStruct result)
        {
            if (!int.TryParse(value, out var val))
            {
                result = default;
                return false;
            }

            result = new TryParseNoFormatProviderStruct(val);
            return true;
        }
    }

    private class BaseTryParseClass<T>
    {
        public static bool TryParse(string? value, out T? result)
        {
            result = default(T);
            return false;
        }
    }

    private class TryParseInheritClass : BaseTryParseClass<TryParseInheritClass>
    {
    }

    // using wrong T on purpose
    private class TryParseWrongTypeInheritClass : BaseTryParseClass<TryParseInheritClass>
    {
    }

    private class BaseTryParseClassWithFormatProvider<T>
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out T? result)
        {
            result = default(T);
            return false;
        }
    }

    private class TryParseInheritClassWithFormatProvider : BaseTryParseClassWithFormatProvider<TryParseInheritClassWithFormatProvider>
    {
    }

    private interface ITryParse<T>
    {
        static bool TryParse(string? value, out T? result)
        {
            result = default(T);
            return false;
        }
    }

    private interface ITryParse2<T>
    {
        static bool TryParse(string? value, out T? result)
        {
            result = default(T);
            return false;
        }
    }

    private interface IImplementITryParse<T> : ITryParse<T>
    {
    }

    private class TryParseFromInterface : ITryParse<TryParseFromInterface>
    {
    }

    private class TryParseFromGrandparentInterface : IImplementITryParse<TryParseFromGrandparentInterface>
    {
    }

    private class TryParseDirectlyAndFromInterface : ITryParse<TryParseDirectlyAndFromInterface>
    {
        static bool TryParse(string? value, out TryParseDirectlyAndFromInterface? result)
        {
            result = null;
            return false;
        }
    }

    private class TryParseFromClassAndInterface
        : BaseTryParseClass<TryParseFromClassAndInterface>,
          ITryParse<TryParseFromClassAndInterface>
    {
    }

    private class TryParseFromMultipleInterfaces
        : ITryParse<TryParseFromMultipleInterfaces>,
          ITryParse2<TryParseFromMultipleInterfaces>
    {
    }

    // using wrong T on purpose
    private class TryParseWrongTypeFromInterface : ITryParse<TryParseFromInterface>
    {
    }

    private interface ITryParseWithFormatProvider<T>
    {
        public static bool TryParse(string? value, IFormatProvider formatProvider, out T? result)
        {
            result = default(T);
            return false;
        }
    }

    private class TryParseFromInterfaceWithFormatProvider : ITryParseWithFormatProvider<TryParseFromInterfaceWithFormatProvider>
    {
    }

    private record BindAsyncRecord(int Value)
    {
        public static ValueTask<BindAsyncRecord?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(BindAsyncRecord), parameter.ParameterType);
            Assert.Equal("bindAsyncRecord", parameter.Name);

            if (!int.TryParse(context.Request.Headers.ETag, out var val))
            {
                return new(result: null);
            }

            return new(result: new(val));
        }
    }

    private record struct BindAsyncStruct(int Value)
    {
        public static ValueTask<BindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            Assert.Equal(typeof(BindAsyncStruct), parameter.ParameterType);
            Assert.Equal("bindAsyncStruct", parameter.Name);

            if (!int.TryParse(context.Request.Headers.ETag, out var val))
            {
                throw new BadHttpRequestException("The request is missing the required ETag header.");
            }

            return new(result: new(val));
        }
    }

    private record struct NullableReturningBindAsyncStruct(int Value)
    {
        public static ValueTask<NullableReturningBindAsyncStruct?> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private record BindAsyncSingleArgRecord(int Value)
    {
        public static ValueTask<BindAsyncSingleArgRecord?> BindAsync(HttpContext context)
        {
            if (!int.TryParse(context.Request.Headers.ETag, out var val))
            {
                return new(result: null);
            }

            return new(result: new(val));
        }
    }

    private record struct BindAsyncSingleArgStruct(int Value)
    {
        public static ValueTask<BindAsyncSingleArgStruct> BindAsync(HttpContext context)
        {
            if (!int.TryParse(context.Request.Headers.ETag, out var val))
            {
                throw new BadHttpRequestException("The request is missing the required ETag header.");
            }

            return new(result: new(val));
        }
    }

    private record struct InvalidWrongReturnBindAsyncStruct(int Value)
    {
        public static Task<InvalidWrongReturnBindAsyncStruct> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private class InvalidWrongReturnBindAsyncClass
    {
        public static Task<InvalidWrongReturnBindAsyncClass> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private record struct InvalidWrongParamBindAsyncStruct(int Value)
    {
        public static ValueTask<InvalidWrongParamBindAsyncStruct> BindAsync(ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private class InvalidWrongParamBindAsyncClass
    {
        public static Task<InvalidWrongParamBindAsyncClass> BindAsync(ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private record struct BindAsyncStructWithGoodAndBad(int Value)
    {
        public static ValueTask<BindAsyncStructWithGoodAndBad> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();

        public static ValueTask<BindAsyncStructWithGoodAndBad> BindAsync(ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private class BindAsyncClassWithGoodAndBad
    {
        public static ValueTask<BindAsyncClassWithGoodAndBad> BindAsync(HttpContext context, ParameterInfo parameter) =>
            throw new NotImplementedException();

        public static ValueTask<BindAsyncClassWithGoodAndBad> BindAsync(ParameterInfo parameter) =>
            throw new NotImplementedException();
    }

    private class BaseBindAsync<T>
    {
        public static ValueTask<T?> BindAsync(HttpContext context)
        {
            return new(default(T));
        }
    }

    private class InheritBindAsync : BaseBindAsync<InheritBindAsync>
    {
    }

    // Using wrong T on purpose
    private class BindAsyncWrongTypeInherit : BaseBindAsync<InheritBindAsync>
    {
    }

    private class BaseBindAsyncWithParameterInfo<T>
    {
        public static ValueTask<T?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return new(default(T));
        }
    }

    private class InheritBindAsyncWithParameterInfo : BaseBindAsyncWithParameterInfo<InheritBindAsyncWithParameterInfo>
    {
    }

    // Using wrong T on purpose
    private class BindAsyncWithParameterInfoWrongTypeInherit : BaseBindAsyncWithParameterInfo<InheritBindAsync>
    {
    }

    private interface IBindAsync<T>
    {
        static ValueTask<T?> BindAsync(HttpContext context)
        {
            return new(default(T));
        }
    }

    private interface IBindAsync2<T>
    {
        static ValueTask<T?> BindAsync(HttpContext context)
        {
            return new(default(T));
        }
    }

    private interface IImeplmentIBindAsync<T> : IBindAsync<T>
    {
    }

    private class BindAsyncFromInterface : IBindAsync<BindAsyncFromInterface>
    {
    }

    private class BindAsyncFromGrandparentInterface : IImeplmentIBindAsync<BindAsyncFromGrandparentInterface>
    {
    }

    private class BindAsyncDirectlyAndFromInterface : IBindAsync<BindAsyncDirectlyAndFromInterface>
    {
        static ValueTask<BindAsyncFromInterface?> BindAsync(HttpContext context)
        {
            return new(result: null);
        }
    }

    private class BindAsyncFromClassAndInterface
        : BaseBindAsync<BindAsyncFromClassAndInterface>,
          IBindAsync<BindAsyncFromClassAndInterface>
    {
    }

    private class BindAsyncFromMultipleInterfaces
        : IBindAsync<BindAsyncFromMultipleInterfaces>,
          IBindAsync2<BindAsyncFromMultipleInterfaces>
    {
    }

    // using wrong T on purpose
    private class BindAsyncWrongTypeFromInterface : IBindAsync<BindAsyncFromInterface>
    {
    }

    private interface IBindAsyncWithParameterInfo<T>
    {
        static ValueTask<T?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return new(default(T));
        }
    }

    private class BindAsyncFromInterfaceWithParameterInfo : IBindAsync<BindAsyncFromInterfaceWithParameterInfo>
    {
    }

    private class BindAsyncFallsBack
    {
        public static void BindAsync(HttpContext context, ParameterInfo parameter)
            => throw new NotImplementedException();

        public static ValueTask<BindAsyncFallsBack?> BindAsync(HttpContext context)
        {
            return new(result: null);
        }
    }

    private class BindAsyncBadMethod : IBindAsyncWithParameterInfo<BindAsyncBadMethod>
    {
        public static void BindAsync(HttpContext context, ParameterInfo parameter)
            => throw new NotImplementedException();
    }

    private class BindAsyncBothBadMethods
    {
        public static void BindAsync(HttpContext context, ParameterInfo parameter)
            => throw new NotImplementedException();

        public static void BindAsync(HttpContext context)
            => throw new NotImplementedException();
    }

    public class ClassWithParameterizedConstructor
    {
        public int Foo { get; set; }

        public ClassWithParameterizedConstructor(int foo)
        {

        }
    }

    public record RecordClassParameterizedConstructor(int Foo);

    public record struct RecordStructParameterizedConstructor(int Foo);

    public struct StructWithParameterizedConstructor
    {
        public int Foo { get; set; }

        public StructWithParameterizedConstructor(int foo)
        {
            Foo = foo;
        }
    }

    public class ClassWithParameterlessConstructor
    {
        public ClassWithParameterlessConstructor()
        {
        }

        public ClassWithParameterlessConstructor(int foo)
        {

        }
    }

    public record RecordClassParameterlessConstructor
    {
        public RecordClassParameterlessConstructor()
        {
        }

        public RecordClassParameterlessConstructor(int foo)
        {

        }
    }

    public struct StructWithParameterlessConstructor
    {
        public StructWithParameterlessConstructor()
        {
        }

        public StructWithParameterlessConstructor(int foo)
        {
        }
    }

    public record struct RecordStructWithParameterlessConstructor
    {
        public RecordStructWithParameterlessConstructor()
        {
        }

        public RecordStructWithParameterlessConstructor(int foo)
        {

        }
    }

    public class ClassWithDefaultConstructor
    { }
    public record RecordClassWithDefaultConstructor
    { }

    public struct StructWithDefaultConstructor
    { }

    public record struct RecordStructWithDefaultConstructor
    { }

    public struct StructWithMultipleConstructors
    {
        public StructWithMultipleConstructors(int foo)
        {
        }
        public StructWithMultipleConstructors(int foo, int bar)
        {
        }
    }

    public record struct RecordStructWithMultipleConstructors(int Foo)
    {
        public RecordStructWithMultipleConstructors(int foo, int bar)
            : this(foo)
        {

        }
    }

    private abstract class AbstractClass { }

    private abstract record AbstractRecord();

    private class ClassWithMultipleConstructors
    {
        public ClassWithMultipleConstructors(int foo)
        { }

        public ClassWithMultipleConstructors(int foo, int bar)
        { }
    }

    private record RecordWithMultipleConstructors
    {
        public RecordWithMultipleConstructors(int foo)
        { }

        public RecordWithMultipleConstructors(int foo, int bar)
        { }
    }

    private class ClassWithInvalidConstructors
    {
        public int Foo { get; set; }

        public ClassWithInvalidConstructors(int foo, int bar)
        { }
    }

    private record RecordClassWithInvalidConstructors
    {
        public int Foo { get; set; }

        public RecordClassWithInvalidConstructors(int foo, int bar)
        { }
    }

    private struct StructWithInvalidConstructors
    {
        public int Foo { get; set; }

        public StructWithInvalidConstructors(int foo, int bar)
        {
            Foo = foo;
        }
    }

    private record struct RecordStructWithInvalidConstructors
    {
        public int Foo { get; set; }

        public RecordStructWithInvalidConstructors(int foo, int bar)
        {
            Foo = foo;
        }
    }

    private class BindAsyncFromImplicitStaticAbstractInterface : IBindableFromHttpContext<BindAsyncFromImplicitStaticAbstractInterface>
    {
        public static ValueTask<BindAsyncFromImplicitStaticAbstractInterface?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return ValueTask.FromResult<BindAsyncFromImplicitStaticAbstractInterface?>(new());
        }
    }

    private class BindAsyncFromExplicitStaticAbstractInterface : IBindableFromHttpContext<BindAsyncFromExplicitStaticAbstractInterface>
    {
        static ValueTask<BindAsyncFromExplicitStaticAbstractInterface?> IBindableFromHttpContext<BindAsyncFromExplicitStaticAbstractInterface>.BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return ValueTask.FromResult<BindAsyncFromExplicitStaticAbstractInterface?>(new());
        }
    }

    private class BindAsyncFromStaticAbstractInterfaceAndBindAsync : IBindableFromHttpContext<BindAsyncFromStaticAbstractInterfaceAndBindAsync>
    {
        public BindAsyncFromStaticAbstractInterfaceAndBindAsync(BindAsyncSource boundFrom)
        {
            BoundFrom = boundFrom;
        }

        public BindAsyncSource BoundFrom { get; }

        // Implicit interface implementation
        public static ValueTask<BindAsyncFromStaticAbstractInterfaceAndBindAsync?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return ValueTask.FromResult<BindAsyncFromStaticAbstractInterfaceAndBindAsync?>(new(BindAsyncSource.InterfaceStaticAbstractImplicit));
        }

        // Late-bound pattern based match in RequestDelegateFactory
        public static ValueTask<BindAsyncFromStaticAbstractInterfaceAndBindAsync?> BindAsync(HttpContext context)
        {
            return ValueTask.FromResult<BindAsyncFromStaticAbstractInterfaceAndBindAsync?>(new(BindAsyncSource.Reflection));
        }
    }

    private class BindAsyncFromStaticAbstractInterfaceWrongType : IBindableFromHttpContext<BindAsyncFromImplicitStaticAbstractInterface>
    {
        public static ValueTask<BindAsyncFromImplicitStaticAbstractInterface?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            return ValueTask.FromResult<BindAsyncFromImplicitStaticAbstractInterface?>(new());
        }
    }

    private enum BindAsyncSource
    {
        Reflection,
        InterfaceStaticAbstractImplicit,
        InterfaceStaticAbstractExplicit
    }

    private class MockParameterInfo : ParameterInfo
    {
        public MockParameterInfo(Type type, string name)
        {
            ClassImpl = type;
            NameImpl = name;
        }
    }

    public class TodoWithExplicitIParsable : IParsable<TodoWithExplicitIParsable>
    {
        static TodoWithExplicitIParsable IParsable<TodoWithExplicitIParsable>.Parse(string s, IFormatProvider? provider)
        {
            throw new NotImplementedException();
        }

        static bool IParsable<TodoWithExplicitIParsable>.TryParse(string? s, IFormatProvider? provider, out TodoWithExplicitIParsable result)
        {
            throw new NotImplementedException();
        }
    }
}
