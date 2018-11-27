// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.Build.Test
{
    // Similar to design time code generation tests, but goes a character at a time.
    // Don't add many of these since they are slow - instead add features to existing
    // tests here, and use these as smoke tests, not for detailed regression testing.
    public class TypingTest : RazorIntegrationTestBase
    {
        internal override bool DesignTime => true;

        internal override bool UseTwoPhaseCompilation => false;

        [Fact]
        public void DoSomeTyping()
        {
            // Arrange
            AdditionalSyntaxTrees.Add(Parse(@"
using System;
using Microsoft.AspNetCore.Components;

namespace Test
{
    public class MyComponent : ComponentBase
    {
        [Parameter] int Value { get; set; }
        [Parameter] Action<int> ValueChanged { get; set; }
        [Parameter] string AnotherValue { get; set; }
    }

    public class ModelState
    {
        public Action<string> Bind(Func<string, string> func) => throw null;
    }
}
"));
            var text = @"
@addTagHelper *, TestAssembly
<div>
  <MyComponent bind-Value=""myValue"" AnotherValue=""hi""/>
  <input type=""text"" bind=""@this.ModelState.Bind(x => x)"" />
  <button ref=""_button"" onsubmit=""@FormSubmitted"">Click me</button>
</div>
<MyComponent 
    IntProperty=""123""
    BoolProperty=""true""
    StringProperty=""My string""
    ObjectProperty=""new SomeType()""/>
@functions {
    Test.ModelState ModelState { get; set; }
}";

            for (var i = 0; i <= text.Length; i++)
            {
                try
                {
                    CompileToCSharp(text.Substring(0, i));
                }
                catch (Exception ex)
                {
                    throw new XunitException($@"
Code generation failed on iteration {i} with source text:
{text.Substring(0, i)}

Exception:
{ex}
");
                }
            }
        }

        [Fact] // Regression test for #1068 
        public void Regression_1068()
        {
            // Arrange

            // Act
            var generated = CompileToCSharp(@"
<input type=""text"" bind="" />
@functions {
    Test.ModelState ModelState { get; set; }
}
");

            // Assert
        }
    }
}
