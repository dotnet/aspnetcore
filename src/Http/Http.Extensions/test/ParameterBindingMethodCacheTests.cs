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

            Assert.NotNull(methodFound.Item1);
            Assert.Equal(2, methodFound.Item2);

            var parsedValue = Expression.Variable(type, "parsedValue");

            var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(
                Expression.Block(new[] { parsedValue }, methodFound.Item1!),
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

            Assert.NotNull(methodFound.Item1);
            Assert.Equal(1, methodFound.Item2);

            var parsedValue = Expression.Variable(type, "parsedValue");

            var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(
                Expression.Block(new[] { parsedValue }, methodFound.Item1!),
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
        private static void BindAsyncNullableSingleArgStructMethod(BindAsyncSingleArgStruct? arg) { }
        private static void NullableReturningBindAsyncSingleArgStructMethod(NullableReturningBindAsyncSingleArgStruct arg) { }

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
