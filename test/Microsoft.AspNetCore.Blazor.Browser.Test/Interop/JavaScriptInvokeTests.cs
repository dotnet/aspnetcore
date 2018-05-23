using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Browser.Interop
{
    public class JavaScriptInvokeTests
    {
        public static TheoryData<object> ResolveMethodPropertyData
        {
            get
            {
                var result = new TheoryData<object>();
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidParameterless)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithOneParameter)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithTwoParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithThreeParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithFourParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithFiveParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithSixParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithSevenParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithEightParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.ReturnArray)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoOneParameter)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoTwoParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoThreeParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoFourParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoFiveParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoSixParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoSevenParameters)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoEightParameters)));

                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidParameterlessAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithOneParameterAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithTwoParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithThreeParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithFourParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithFiveParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithSixParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithSevenParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.VoidWithEightParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.ReturnArrayAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoOneParameterAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoTwoParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoThreeParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoFourParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoFiveParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoSixParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoSevenParametersAsync)));
                result.Add(CreateMethodOptions(nameof(JavaScriptInterop.EchoEightParametersAsync)));

                return result;

                MethodInvocationOptions CreateMethodOptions(string methodName) =>
                    new MethodInvocationOptions
                    {
                        Type = new TypeIdentifier
                        {
                            Assembly = typeof(JavaScriptInterop).Assembly.GetName().Name,
                            Name = typeof(JavaScriptInterop).FullName
                        },
                        Method = new MethodIdentifier
                        {
                            Name = methodName
                        }
                    };
            }
        }

        [Theory]
        [MemberData(nameof(ResolveMethodPropertyData))]
        public void ResolveMethod(object optionsObject)
        {
            var options = optionsObject as MethodInvocationOptions;

            var resolvedMethod = options.GetMethodOrThrow();

            Assert.NotNull(resolvedMethod);
            Assert.Equal(options.Method.Name, resolvedMethod.Name);
        }
    }

    internal class JavaScriptInterop
    {
        public static IDictionary<string, object[]> Invocations = new Dictionary<string, object[]>();

        public static void VoidParameterless()
        {
            Invocations[nameof(VoidParameterless)] = new object[0];
        }

        public static void VoidWithOneParameter(ComplexParameter parameter1)
        {
            Invocations[nameof(VoidWithOneParameter)] = new object[] { parameter1 };
        }

        public static void VoidWithTwoParameters(
            ComplexParameter parameter1,
            byte parameter2)
        {
            Invocations[nameof(VoidWithTwoParameters)] = new object[] { parameter1, parameter2 };
        }

        public static void VoidWithThreeParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3)
        {
            Invocations[nameof(VoidWithThreeParameters)] = new object[] { parameter1, parameter2, parameter3 };
        }

        public static void VoidWithFourParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4)
        {
            Invocations[nameof(VoidWithFourParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4 };
        }

        public static void VoidWithFiveParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5)
        {
            Invocations[nameof(VoidWithFiveParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5 };
        }

        public static void VoidWithSixParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            Invocations[nameof(VoidWithSixParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6 };
        }

        public static void VoidWithSevenParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            Invocations[nameof(VoidWithSevenParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7 };
        }

        public static void VoidWithEightParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            Invocations[nameof(VoidWithEightParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8 };
        }

        public static decimal[] ReturnArray()
        {
            return new decimal[] { 0.1M, 0.2M };
        }

        public static object[] EchoOneParameter(ComplexParameter parameter1)
        {
            return new object[] { parameter1 };
        }

        public static object[] EchoTwoParameters(
            ComplexParameter parameter1,
            byte parameter2)
        {
            return new object[] { parameter1, parameter2 };
        }

        public static object[] EchoThreeParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3)
        {
            return new object[] { parameter1, parameter2, parameter3 };
        }

        public static object[] EchoFourParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4)
        {
            return new object[] { parameter1, parameter2, parameter3, parameter4 };
        }

        public static object[] EchoFiveParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5)
        {
            return new object[] { parameter1, parameter2, parameter3, parameter4, parameter5 };
        }

        public static object[] EchoSixParameters(ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            return new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6 };
        }

        public static object[] EchoSevenParameters(ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            return new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7 };
        }

        public static object[] EchoEightParameters(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            return new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8 };
        }

        public static Task VoidParameterlessAsync()
        {
            Invocations[nameof(VoidParameterlessAsync)] = new object[0];
            return Task.CompletedTask;
        }

        public static Task VoidWithOneParameterAsync(ComplexParameter parameter1)
        {
            Invocations[nameof(VoidParameterless)] = new object[] { parameter1 };
            return Task.CompletedTask;
        }

        public static Task VoidWithTwoParametersAsync(
            ComplexParameter parameter1,
            byte parameter2)
        {
            Invocations[nameof(VoidParameterless)] = new object[] { parameter1, parameter2 };
            return Task.CompletedTask;
        }

        public static Task VoidWithThreeParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3)
        {
            Invocations[nameof(VoidWithThreeParameters)] = new object[] { parameter1, parameter2, parameter3 };
            return Task.CompletedTask;
        }

        public static Task VoidWithFourParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4)
        {
            Invocations[nameof(VoidWithFourParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4 };
            return Task.CompletedTask;
        }

        public static Task VoidWithFiveParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5)
        {
            Invocations[nameof(VoidWithFiveParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5 };
            return Task.CompletedTask;
        }

        public static Task VoidWithSixParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            Invocations[nameof(VoidWithSixParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6 };
            return Task.CompletedTask;
        }

        public static Task VoidWithSevenParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            Invocations[nameof(VoidWithSevenParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7 };
            return Task.CompletedTask;
        }

        public static Task VoidWithEightParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            Invocations[nameof(VoidWithEightParameters)] = new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8 };
            return Task.CompletedTask;
        }

        public static Task<decimal[]> ReturnArrayAsync()
        {
            return Task.FromResult(new decimal[] { 0.1M, 0.2M });
        }

        public static Task<object[]> EchoOneParameterAsync(ComplexParameter parameter1)
        {
            return Task.FromResult(new object[] { parameter1 });
        }

        public static Task<object[]> EchoTwoParametersAsync(
            ComplexParameter parameter1,
            byte parameter2)
        {
            return Task.FromResult(new object[] { parameter1, parameter2 });
        }

        public static Task<object[]> EchoThreeParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3 });
        }

        public static Task<object[]> EchoFourParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3, parameter4 });
        }

        public static Task<object[]> EchoFiveParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3, parameter4, parameter5 });
        }

        public static Task<object[]> EchoSixParametersAsync(ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6 });
        }

        public static Task<object[]> EchoSevenParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7 });
        }

        public static Task<object[]> EchoEightParametersAsync(
            ComplexParameter parameter1,
            byte parameter2,
            short parameter3,
            int parameter4,
            long parameter5,
            float parameter6,
            List<double> parameter7,
            Segment parameter8)
        {
            return Task.FromResult(new object[] { parameter1, parameter2, parameter3, parameter4, parameter5, parameter6, parameter7, parameter8 });
        }
    }

    public struct Segment
    {
        public string Source { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    }

    public class ComplexParameter
    {
        public int Id { get; set; }
        public bool IsValid { get; set; }
        public Segment Data { get; set; }
    }
}
