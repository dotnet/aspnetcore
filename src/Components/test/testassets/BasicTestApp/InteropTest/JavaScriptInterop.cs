// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BasicTestApp.InteropTest
{
    public class JavaScriptInterop
    {
        public static IDictionary<string, object[]> Invocations = new Dictionary<string, object[]>();

        [JSInvokable]
        public static void ThrowException() => throw new InvalidOperationException("Threw an exception!");

        [JSInvokable]
        public static Task AsyncThrowSyncException()
            => throw new InvalidOperationException("Threw a sync exception!");

        [JSInvokable]
        public static async Task AsyncThrowAsyncException()
        {
            await Task.Yield();
            throw new InvalidOperationException("Threw an async exception!");
        }

        [JSInvokable]
        public static void VoidParameterless()
        {
            Invocations[nameof(VoidParameterless)] = new object[0];
        }

        [JSInvokable]
        public static void VoidWithOneParameter(ComplexParameter parameter1)
        {
            Invocations[nameof(VoidWithOneParameter)] = new object[] { parameter1 };
        }

        [JSInvokable]
        public static void VoidWithTwoParameters(
            ComplexParameter parameter1,
            byte parameter2)
        {
            Invocations[nameof(VoidWithTwoParameters)] = new object[] { parameter1, parameter2 };
        }

        [JSInvokable]
        public static void VoidWithThreeParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3)
        {
            Invocations[nameof(VoidWithThreeParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue() };
        }

        [JSInvokable]
        public static void VoidWithFourParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4)
        {
            Invocations[nameof(VoidWithFourParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4 };
        }

        [JSInvokable]
        public static void VoidWithFiveParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5)
        {
            Invocations[nameof(VoidWithFiveParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5 };
        }

        [JSInvokable]
        public static void VoidWithSixParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            Invocations[nameof(VoidWithSixParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6 };
        }

        [JSInvokable]
        public static void VoidWithSevenParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            Invocations[nameof(VoidWithSevenParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7 };
        }

        [JSInvokable]
        public static void VoidWithEightParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            Invocations[nameof(VoidWithEightParameters)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7, parameter8 };
        }

        [JSInvokable]
        public static decimal[] ReturnArray()
        {
            return new decimal[] { 0.1M, 0.2M };
        }

        [JSInvokable]
        public static object[] EchoOneParameter(ComplexParameter parameter1)
        {
            return new object[] { parameter1 };
        }

        [JSInvokable]
        public static object[] EchoTwoParameters(
            ComplexParameter parameter1,
            byte parameter2)
        {
            return new object[] { parameter1, parameter2 };
        }

        [JSInvokable]
        public static object[] EchoThreeParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue() };
        }

        [JSInvokable]
        public static object[] EchoFourParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4 };
        }

        [JSInvokable]
        public static object[] EchoFiveParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5 };
        }

        [JSInvokable]
        public static object[] EchoSixParameters(ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6 };
        }

        [JSInvokable]
        public static object[] EchoSevenParameters(ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7 };
        }

        [JSInvokable]
        public static object[] EchoEightParameters(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            return new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7, parameter8 };
        }

        [JSInvokable]
        public static Task VoidParameterlessAsync()
        {
            Invocations[nameof(VoidParameterlessAsync)] = new object[0];
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithOneParameterAsync(ComplexParameter parameter1)
        {
            Invocations[nameof(VoidWithOneParameterAsync)] = new object[] { parameter1 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithTwoParametersAsync(
            ComplexParameter parameter1,
            byte parameter2)
        {
            Invocations[nameof(VoidWithTwoParametersAsync)] = new object[] { parameter1, parameter2 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithThreeParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3)
        {
            Invocations[nameof(VoidWithThreeParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue() };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithFourParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4)
        {
            Invocations[nameof(VoidWithFourParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithFiveParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5)
        {
            Invocations[nameof(VoidWithFiveParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithSixParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            Invocations[nameof(VoidWithSixParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithSevenParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            Invocations[nameof(VoidWithSevenParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task VoidWithEightParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            Invocations[nameof(VoidWithEightParametersAsync)] = new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7, parameter8 };
            return Task.CompletedTask;
        }

        [JSInvokable]
        public static Task<decimal[]> ReturnArrayAsync()
        {
            return Task.FromResult(new decimal[] { 0.1M, 0.2M });
        }

        [JSInvokable]
        public static Task<object[]> EchoOneParameterAsync(ComplexParameter parameter1)
        {
            return Task.FromResult(new object[] { parameter1 });
        }

        [JSInvokable]
        public static Task<object[]> EchoTwoParametersAsync(
            ComplexParameter parameter1,
            byte parameter2)
        {
            return Task.FromResult(new object[] { parameter1, parameter2 });
        }

        [JSInvokable]
        public static Task<object[]> EchoThreeParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue() });
        }

        [JSInvokable]
        public static Task<object[]> EchoFourParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4 });
        }

        [JSInvokable]
        public static Task<object[]> EchoFiveParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5 });
        }

        [JSInvokable]
        public static Task<object[]> EchoSixParametersAsync(ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6 });
        }

        [JSInvokable]
        public static Task<object[]> EchoSevenParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7 });
        }

        [JSInvokable]
        public static Task<object[]> EchoEightParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            TestDTO parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3.GetNonSerializedValue(), parameter4, parameter5, parameter6, parameter7, parameter8 });
        }

        [JSInvokable]
        public static Dictionary<string, object> ReturnDotNetObjectByRef()
        {
            return new Dictionary<string, object>
            {
                { "Some sync instance", new DotNetObjectRef(new TestDTO(1000)) }
            };
        }

        [JSInvokable]
        public static async Task<Dictionary<string, object>> ReturnDotNetObjectByRefAsync()
        {
            await Task.Yield();
            return new Dictionary<string, object>
            {
                { "Some async instance", new DotNetObjectRef(new TestDTO(1001)) }
            };
        }

        [JSInvokable]
        public static int ExtractNonSerializedValue(TestDTO objectByRef)
        {
            return objectByRef.GetNonSerializedValue();
        }

        [JSInvokable]
        public Dictionary<string, object> InstanceMethod(Dictionary<string, object> dict)
        {
            // This method shows we can pass in values marshalled both as JSON (the dict itself)
            // and by ref (the incoming dtoByRef), plus that we can return values marshalled as
            // JSON (the returned dictionary) and by ref (the outgoingByRef value)
            return new Dictionary<string, object>
            {
                { "thisTypeName", GetType().Name },
                { "stringValueUpper", ((string)dict["stringValue"]).ToUpperInvariant() },
                { "incomingByRef", ((TestDTO)dict["dtoByRef"]).GetNonSerializedValue() },
                { "outgoingByRef", new DotNetObjectRef(new TestDTO(1234)) },
            };
        }

        [JSInvokable]
        public async Task<Dictionary<string, object>> InstanceMethodAsync(Dictionary<string, object> dict)
        {
            // This method shows we can pass in values marshalled both as JSON (the dict itself)
            // and by ref (the incoming dtoByRef), plus that we can return values marshalled as
            // JSON (the returned dictionary) and by ref (the outgoingByRef value)
            await Task.Yield();
            return new Dictionary<string, object>
            {
                { "thisTypeName", GetType().Name },
                { "stringValueUpper", ((string)dict["stringValue"]).ToUpperInvariant() },
                { "incomingByRef", ((TestDTO)dict["dtoByRef"]).GetNonSerializedValue() },
                { "outgoingByRef", new DotNetObjectRef(new TestDTO(1234)) },
            };
        }
    }
}
