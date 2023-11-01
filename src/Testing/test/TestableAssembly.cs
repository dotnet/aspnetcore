// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

/* Creates a very simple dynamic assembly containing
 *
 * [Assembly: TestFramework(
 *     typeName: "Microsoft.AspNetCore.InternalTesting.AspNetTestFramework",
 *     assemblyName: "Microsoft.AspNetCore.InternalTesting")]
 * [assembly: AssemblyFixture(typeof({fixtureType}))]
 * [assembly: TestOutputDirectory(
 *     preserveExistingLogsInOutput: "false",
 *     targetFramework: TFM,
 *     baseDirectory: logDirectory)] // logdirectory is passed into Create(...).
 *
 * public class MyTestClass
 * {
 *     public MyTestClass() { }
 *
 *     [Fact]
 *     public MyTestMethod()
 *     {
 *         if (failTestCase) // Not exactly; condition checked during generation.
 *         {
 *             Assert.True(condition: false);
 *         }
 *     }
 * }
 */
public static class TestableAssembly
{
    public static readonly Assembly ThisAssembly = typeof(TestableAssembly).GetTypeInfo().Assembly;
    public static readonly string ThisAssemblyName = ThisAssembly.GetName().Name;

    private static readonly TestOutputDirectoryAttribute ThisOutputDirectoryAttribute =
        ThisAssembly.GetCustomAttributes().OfType<TestOutputDirectoryAttribute>().FirstOrDefault();
    public static readonly string BaseDirectory = ThisOutputDirectoryAttribute.BaseDirectory;
    public static readonly string TFM = ThisOutputDirectoryAttribute.TargetFramework;

    public const string TestClassName = "MyTestClass";
    public const string TestMethodName = "MyTestMethod";

    public static Assembly Create(Type fixtureType, string logDirectory = null, bool failTestCase = false)
    {
        var frameworkConstructor = typeof(TestFrameworkAttribute)
            .GetConstructor(new[] { typeof(string), typeof(string) });
        var frameworkBuilder = new CustomAttributeBuilder(
            frameworkConstructor,
            new[] { "Microsoft.AspNetCore.InternalTesting.AspNetTestFramework", "Microsoft.AspNetCore.InternalTesting" });

        var fixtureConstructor = typeof(AssemblyFixtureAttribute).GetConstructor(new[] { typeof(Type) });
        var fixtureBuilder = new CustomAttributeBuilder(fixtureConstructor, new[] { fixtureType });

        var outputConstructor = typeof(TestOutputDirectoryAttribute).GetConstructor(
            new[] { typeof(string), typeof(string), typeof(string) });
        var outputBuilder = new CustomAttributeBuilder(outputConstructor, new[] { "false", TFM, logDirectory });

        var testAssemblyName = $"Test{Guid.NewGuid():n}";
        var assemblyName = new AssemblyName(testAssemblyName);
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            assemblyName,
            AssemblyBuilderAccess.Run,
            new[] { frameworkBuilder, fixtureBuilder, outputBuilder });

        var module = assembly.DefineDynamicModule(testAssemblyName);
        var type = module.DefineType(TestClassName, TypeAttributes.Public);
        type.DefineDefaultConstructor(MethodAttributes.Public);

        var method = type.DefineMethod(TestMethodName, MethodAttributes.Public);
        var factConstructor = typeof(FactAttribute).GetConstructor(Array.Empty<Type>());
        var factBuilder = new CustomAttributeBuilder(factConstructor, Array.Empty<object>());
        method.SetCustomAttribute(factBuilder);

        var generator = method.GetILGenerator();
        if (failTestCase)
        {
            // Assert.True(condition: false);
            generator.Emit(OpCodes.Ldc_I4_0);
            var trueInfo = typeof(Assert).GetMethod("True", new[] { typeof(bool) });
            generator.EmitCall(OpCodes.Call, trueInfo, optionalParameterTypes: null);
        }

        generator.Emit(OpCodes.Ret);
        type.CreateType();

        return assembly;
    }
}
