// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Globalization;
using System.Linq.Expressions;

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
        public void FindTryParseMethod_ReturnsTheExpectedTryParseMethodWithInvariantCulture(Type @type)
        {
            var methodFound = new TryParseMethodCache().FindTryParseMethod(@type);

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
        public void FindTryParseMethod_ReturnsTheExpectedTryParseMethodWithInvariantCultureDateType(Type @type)
        {
            var methodFound = new TryParseMethodCache().FindTryParseMethod(@type);

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
        [InlineData(typeof(TryParsableInvariantRecord))]
        public void FindTryParseMethod_ReturnsTheExpectedTryParseMethodWithInvariantCultureCustomType(Type @type)
        {
            var methodFound = new TryParseMethodCache().FindTryParseMethod(@type);

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

        [Fact]
        public void FindTryParseMethodForEnums()
        {
            var type = typeof(Choice);
            var methodFound = new TryParseMethodCache().FindTryParseMethod(type);

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
        public void FindTryParseMethodForEnumsWhenNonGenericEnumParseIsUsed()
        {
            var type = typeof(Choice);
            var cache = new TryParseMethodCache(preferNonGenericEnumParseOverload: true);
            var methodFound = cache.FindTryParseMethod(type);

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

        enum Choice
        {
            One,
            Two,
            Three
        }

        private record TryParsableInvariantRecord(int value)
        {
            public static bool TryParse(string? value, IFormatProvider formatProvider, out TryParsableInvariantRecord? result)
            {
                if (!int.TryParse(value, NumberStyles.Integer, formatProvider, out var val))
                {
                    result = null;
                    return false;
                }

                result = new TryParsableInvariantRecord(val);
                return true;
            }
        }

    }
}
