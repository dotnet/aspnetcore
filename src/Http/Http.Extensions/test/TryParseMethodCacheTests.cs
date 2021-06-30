// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
            var methodFound = TryParseMethodCache.FindTryParseMethod(@type);

            Assert.NotNull(methodFound);

            var call = methodFound!(Expression.Variable(type, "parsedValue"));
            var parameters = call.Method.GetParameters();

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
            var methodFound = TryParseMethodCache.FindTryParseMethod(@type);

            Assert.NotNull(methodFound);

            var call = methodFound!(Expression.Variable(type, "parsedValue"));
            var parameters = call.Method.GetParameters();

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
            var methodFound = TryParseMethodCache.FindTryParseMethod(@type);

            Assert.NotNull(methodFound);

            var call = methodFound!(Expression.Variable(type, "parsedValue"));
            var parameters = call.Method.GetParameters();

            Assert.Equal(3, parameters.Length);
            Assert.Equal(typeof(string), parameters[0].ParameterType);
            Assert.Equal(typeof(IFormatProvider), parameters[1].ParameterType);
            Assert.True(parameters[2].IsOut);
            Assert.True(((call.Arguments[1] as ConstantExpression)!.Value as CultureInfo)!.Equals(CultureInfo.InvariantCulture));
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
