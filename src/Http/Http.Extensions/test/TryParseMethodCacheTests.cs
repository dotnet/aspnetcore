// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Microsoft.AspNetCore.Http.Extensions.Tests
{
    public class TryParseMethodCacheTests
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
            var methodFound = new TryParseMethodCache().FindTryParseStringMethod(@type);

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
            var methodFound = new TryParseMethodCache().FindTryParseStringMethod(@type);

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
            var methodFound = new TryParseMethodCache().FindTryParseStringMethod(@type);

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
            Assert.True(new TryParseMethodCache().HasTryParseStringMethod(parameterInfo));
        }

        [Fact]
        public void FindTryParseStringMethod_WorksForEnums()
        {
            var type = typeof(Choice);
            var methodFound = new TryParseMethodCache().FindTryParseStringMethod(type);

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
            var cache = new TryParseMethodCache(preferNonGenericEnumParseOverload: true);
            var methodFound = cache.FindTryParseStringMethod(type);

            Assert.NotNull(methodFound);

            var parsedValue = Expression.Variable(type, "parsedValue");
            var block = methodFound!(parsedValue) as BlockExpression;
            Assert.NotNull(block);
            Assert.Equal(typeof(bool), block!.Type);

            var parseEnum = Expression.Lambda<Func<string, Choice>>(Expression.Block(new[] { parsedValue },
                block,
                parsedValue), cache.TempSourceStringExpr).Compile();

            Assert.Equal(Choice.One, parseEnum("One"));
            Assert.Equal(Choice.Two, parseEnum("Two"));
            Assert.Equal(Choice.Three, parseEnum("Three"));
        }

        [Fact]
        public async Task FindBindAsyncMethod_FindsCorrectMethodOnClass()
        {
            var type = typeof(BindAsyncRecord);
            var cache = new TryParseMethodCache();
            var methodFound = cache.FindBindAsyncMethod(type);

            Assert.NotNull(methodFound);

            var parsedValue = Expression.Variable(type, "parsedValue");
            var call = methodFound as MethodCallExpression;
            Assert.NotNull(call);
            var method = call!.Method;
            var parameters = method.GetParameters();

            Assert.Single(parameters);
            Assert.Equal(typeof(HttpContext), parameters[0].ParameterType);

            var parseHttpContext = Expression.Lambda<Func<HttpContext, ValueTask<object>>>(Expression.Block(new[] { parsedValue },
                call), cache.HttpContextExpr).Compile();

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
                };
            }
        }

        [Theory]
        [MemberData(nameof(BindAsyncParameterInfoData))]
        public void HasBindAsyncMethod_ReturnsTrueWhenMethodExists(ParameterInfo parameterInfo)
        {
            Assert.True(new TryParseMethodCache().HasBindAsyncMethod(parameterInfo));
        }

        [Fact]
        public void FindBindAsyncMethod_DoesNotFindMethodGivenNullableType()
        {
            var parameterInfo = GetFirstParameter((BindAsyncStruct? arg) => BindAsyncNullableStructMethod(arg));
            Assert.False(new TryParseMethodCache().HasBindAsyncMethod(parameterInfo));
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
            public static ValueTask<object?> BindAsync(HttpContext context)
            {
                if (!int.TryParse(context.Request.Headers.ETag, out var val))
                {
                    return ValueTask.FromResult<object?>(null);
                }

                return ValueTask.FromResult<object?>(new BindAsyncRecord(val));
            }
        }

        private record struct BindAsyncStruct(int Value)
        {
            public static ValueTask<object?> BindAsync(HttpContext context)
            {
                if (!int.TryParse(context.Request.Headers.ETag, out var val))
                {
                    return ValueTask.FromResult<object?>(null);
                }

                return ValueTask.FromResult<object?>(new BindAsyncRecord(val));
            }
        }
    }
}
