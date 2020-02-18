// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.Hosting
{
    public class EntrypointInvokerTest
    {
        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InvokesEntrypoint_Sync_Success(bool hasReturnValue, bool hasParams)
        {
            // Arrange
            var returnType = hasReturnValue ? "int" : "void";
            var paramsDecl = hasParams ? "string[] args" : string.Empty;
            var returnStatement = hasReturnValue ? "return 123;" : "return;";
            var assembly = CompileToAssembly(@"
static " + returnType + @" Main(" + paramsDecl + @")
{
    DidMainExecute = true;
    " + returnStatement + @"
}", out var didMainExecute);

            // Act
            EntrypointInvoker.InvokeEntrypoint(assembly.FullName, new string[] { });

            // Assert
            Assert.True(didMainExecute());
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void InvokesEntrypoint_Async_Success(bool hasReturnValue, bool hasParams)
        {
            // Arrange
            var returnTypeGenericParam = hasReturnValue ? "<int>" : string.Empty;
            var paramsDecl = hasParams ? "string[] args" : string.Empty;
            var returnStatement = hasReturnValue ? "return 123;" : "return;";
            var assembly = CompileToAssembly(@"
public static TaskCompletionSource<object> ContinueTcs { get; } = new TaskCompletionSource<object>();

static async Task" + returnTypeGenericParam + @" Main(" + paramsDecl + @")
{
    await ContinueTcs.Task;
    DidMainExecute = true;
    " + returnStatement + @"
}", out var didMainExecute);

            // Act/Assert 1: Waits for task
            // The fact that we're not blocking here proves that we're not executing the
            // metadata-declared entrypoint, as that would block
            EntrypointInvoker.InvokeEntrypoint(assembly.FullName, new string[] { });
            Assert.False(didMainExecute());

            // Act/Assert 2: Continues
            var tcs = (TaskCompletionSource<object>)assembly.GetType("SomeApp.Program").GetProperty("ContinueTcs").GetValue(null);
            tcs.SetResult(null);
            Assert.True(didMainExecute());
        }

        [Fact]
        public void InvokesEntrypoint_Sync_Exception()
        {
            // Arrange
            var assembly = CompileToAssembly(@"
public static void Main()
{
    DidMainExecute = true;
    throw new InvalidTimeZoneException(""Test message"");
}", out var didMainExecute);

            // Act/Assert
            // The fact that this doesn't throw shows that EntrypointInvoker is doing something
            // to handle the exception. We can't assert about what it does here, because that
            // would involve capturing console output, which isn't safe in unit tests. Instead
            // we'll check this in E2E tests.
            EntrypointInvoker.InvokeEntrypoint(assembly.FullName, new string[] { });
            Assert.True(didMainExecute());
        }

        [Fact]
        public void InvokesEntrypoint_Async_Exception()
        {
            // Arrange
            var assembly = CompileToAssembly(@"
public static TaskCompletionSource<object> ContinueTcs { get; } = new TaskCompletionSource<object>();

public static async Task Main()
{
    await ContinueTcs.Task;
    DidMainExecute = true;
    throw new InvalidTimeZoneException(""Test message"");
}", out var didMainExecute);

            // Act/Assert 1: Waits for task
            EntrypointInvoker.InvokeEntrypoint(assembly.FullName, new string[] { });
            Assert.False(didMainExecute());

            // Act/Assert 2: Continues
            // As above, we can't directly observe the exception handling behavior here,
            // so this is covered in E2E tests instead.
            var tcs = (TaskCompletionSource<object>)assembly.GetType("SomeApp.Program").GetProperty("ContinueTcs").GetValue(null);
            tcs.SetResult(null);
            Assert.True(didMainExecute());
        }

        private static Assembly CompileToAssembly(string mainMethod, out Func<bool> didMainExecute)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(@"
using System;
using System.Threading.Tasks;

namespace SomeApp
{
    public static class Program
    {
        public static bool DidMainExecute { get; private set; }

        " + mainMethod + @"
    }
}");

            var compilation = CSharpCompilation.Create(
                $"TestAssembly-{Guid.NewGuid().ToString("D")}",
                new[] { syntaxTree },
                new[] { MetadataReference.CreateFromFile(typeof(object).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
            using var ms = new MemoryStream();
            var compilationResult = compilation.Emit(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

            var didMainExecuteProp = assembly.GetType("SomeApp.Program").GetProperty("DidMainExecute");
            didMainExecute = () => (bool)didMainExecuteProp.GetValue(null); 

            return assembly;
        }
    }
}
