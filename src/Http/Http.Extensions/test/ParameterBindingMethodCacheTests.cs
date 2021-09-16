// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Http.Extensions.Tests
{
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

            var call = methodFound!(Expression.Variable(type, "parsedValue")) as MethodCallExpression;
            Assert.NotNull(call);
            var parameters = call!.Method.GetParameters();

            Assert.Equal(4, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(NumberStyles), parameters[1].ParameterType);
            Assert.Equal(typeof(IFormatProvider), parameters[2].ParameterType);
            Assert.True(parameters[3].IsOut);
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

            var call = methodFound!(Expression.Variable(type, "parsedValue")) as MethodCallExpression;
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
        public void FindTryParseStringMethod_ReturnsTheExpectedTryParseMethodWithInvariantCultureCustomType(Type type)
        {
            var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(@type);

            Assert.NotNull(methodFound);

            var call = methodFound!(Expression.Variable(type, "parsedValue")) as MethodCallExpression;
            Assert.NotNull(call);
            var parameters = call!.Method.GetParameters();

            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(IFormatProvider), parameters[1].ParameterType);
            Assert.True(parameters[2].IsOut);
            Assert.True(((call.Arguments[1] as ConstantExpression)!.Value as CultureInfo)!.Equals(CultureInfo.InvariantCulture));
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
            Assert.True(new ParameterBindingMethodCache().HasTryParseMethod(parameterInfo));
        }

        [Fact]
        public void FindTryParseStringMethod_WorksForEnums()
        {
            var type = typeof(Choice);
            var methodFound = new ParameterBindingMethodCache().FindTryParseMethod(type);

            Assert.NotNull(methodFound);

            var call = methodFound!(Expression.Variable(type, "parsedValue")) as MethodCallExpression;
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
            var block = methodFound!(parsedValue) as BlockExpression;
            Assert.NotNull(block);
            Assert.Equal(typeof(bool), block!.Type);

            var parseEnum = Expression.Lambda<Func<string, Choice>>(Expression.Block(new[] { parsedValue },
                block,
                parsedValue), ParameterBindingMethodCache.TempSourceStringExpr).Compile();

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
                ParameterBindingMethodCache.HttpContextExpr).Compile();

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
                ParameterBindingMethodCache.HttpContextExpr).Compile();

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
                    }
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
        public void FindBindAsyncMethod_FindsNonNullableReturningBindAsyncMethodGivenNullableType()
        {
            var parameterInfo = GetFirstParameter((BindAsyncStruct? arg) => BindAsyncNullableStructMethod(arg));
            Assert.True(new ParameterBindingMethodCache().HasBindAsyncMethod(parameterInfo));
        }

        [Theory]
        [InlineData(typeof(InvalidVoidReturnTryParseStruct))]
        [InlineData(typeof(InvalidVoidReturnTryParseClass))]
        [InlineData(typeof(InvalidWrongTypeTryParseStruct))]
        [InlineData(typeof(InvalidWrongTypeTryParseClass))]
        [InlineData(typeof(InvalidTryParseNullableStruct))]
        [InlineData(typeof(InvalidTooFewArgsTryParseStruct))]
        [InlineData(typeof(InvalidTooFewArgsTryParseClass))]
        [InlineData(typeof(InvalidNonStaticTryParseStruct))]
        [InlineData(typeof(InvalidNonStaticTryParseClass))]
        public void FindTryParseMethod_ThrowsIfInvalidTryParseOnType(Type type)
        {
            var ex = Assert.Throws<InvalidOperationException>(
                () => new ParameterBindingMethodCache().FindTryParseMethod(type));
            Assert.StartsWith($"TryParse method found on {type.Name} with incorrect format. Must be a static method with format", ex.Message);
            Assert.Contains($"bool TryParse(string, IFormatProvider, out {type.Name})", ex.Message);
            Assert.Contains($"bool TryParse(string, out {type.Name})", ex.Message);
        }

        [Theory]
        [InlineData(typeof(TryParseClassWithGoodAndBad))]
        [InlineData(typeof(TryParseStructWithGoodAndBad))]
        public void FindTryParseMethod_IgnoresInvalidTryParseIfGoodOneFound(Type type)
        {
            var method = new ParameterBindingMethodCache().FindTryParseMethod(type);
            Assert.NotNull(method);
        }

        [Theory]
        [InlineData(typeof(InvalidWrongReturnBindAsyncStruct))]
        [InlineData(typeof(InvalidWrongReturnBindAsyncClass))]
        [InlineData(typeof(InvalidWrongParamBindAsyncStruct))]
        [InlineData(typeof(InvalidWrongParamBindAsyncClass))]
        public void FindBindAsyncMethod_ThrowsIfInvalidBindAsyncOnType(Type type)
        {
            var cache = new ParameterBindingMethodCache();
            var parameter = new MockParameterInfo(type, "anything");
            var ex = Assert.Throws<InvalidOperationException>(
                () => cache.FindBindAsyncMethod(parameter));
            Assert.StartsWith($"BindAsync method found on {type.Name} with incorrect format. Must be a static method with format", ex.Message);
            Assert.Contains($"ValueTask<{type.Name}> BindAsync(HttpContext context, ParameterInfo parameter)", ex.Message);
            Assert.Contains($"ValueTask<{type.Name}> BindAsync(HttpContext context)", ex.Message);
            Assert.Contains($"ValueTask<{type.Name}?> BindAsync(HttpContext context, ParameterInfo parameter)", ex.Message);
            Assert.Contains($"ValueTask<{type.Name}?> BindAsync(HttpContext context)", ex.Message);
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

        private static ParameterInfo GetFirstParameter<T>(Expression<Action<T>> expr)
        {
            var mc = (MethodCallExpression)expr.Body;
            return mc.Method.GetParameters()[0];
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

        private record struct NullableReturningBindAsyncSingleArgStruct(int Value)
        {
            public static ValueTask<NullableReturningBindAsyncStruct?> BindAsync(HttpContext context, ParameterInfo parameter) =>
                throw new NotImplementedException();
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
            public static ValueTask<InvalidWrongReturnBindAsyncStruct> BindAsync(ParameterInfo parameter) =>
                throw new NotImplementedException();
        }

        private class InvalidWrongParamBindAsyncClass
        {
            public static Task<InvalidWrongReturnBindAsyncClass> BindAsync(ParameterInfo parameter) =>
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

        private class MockParameterInfo : ParameterInfo
        {
            public MockParameterInfo(Type type, string name)
            {
                ClassImpl = type;
                NameImpl = name;
            }
        }
    }
}
