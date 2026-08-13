// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Analyzers;

public static class ComponentsTestDeclarations
{
    public static readonly string Source = $@"
    namespace {typeof(ParameterAttribute).Namespace}
    {{
        public class {typeof(ParameterAttribute).Name} : System.Attribute
        {{
            public bool CaptureUnmatchedValues {{ get; set; }}
        }}

        public class {typeof(CascadingParameterAttribute).Name} : System.Attribute
        {{
        }}

        public interface {typeof(IComponent).Name}
        {{
        }}
    }}
";
    public static readonly string SourceWithJSInvokable = """
        namespace Microsoft.JSInterop
        {
            public sealed class JSInvokableAttribute : System.Attribute
            {
                public string? Identifier { get; }

                public JSInvokableAttribute()
                {
                }

                public JSInvokableAttribute(string identifier)
                {
                }
            }
        }
        """;
}
