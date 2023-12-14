// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal partial class HubServerProxyGenerator
{
    public sealed class Emitter
    {
        private readonly SourceProductionContext _context;
        private readonly SourceGenerationSpec _spec;

        public Emitter(SourceProductionContext context, SourceGenerationSpec spec)
        {
            _context = context;
            _spec = spec;
        }

        public void Emit()
        {
            if (string.IsNullOrEmpty(_spec.GetterClassAccessibility) ||
                string.IsNullOrEmpty(_spec.GetterMethodAccessibility) ||
                string.IsNullOrEmpty(_spec.GetterClassName) ||
                string.IsNullOrEmpty(_spec.GetterMethodName) ||
                string.IsNullOrEmpty(_spec.GetterTypeParameterName) ||
                string.IsNullOrEmpty(_spec.GetterHubConnectionParameterName))
            {
                return;
            }

            // Generate extensions and other user facing mostly-static code in a single source file
            EmitExtensions();
            // Generate hub proxy code in its own source file for each hub proxy type
            foreach (var classSpec in _spec.Classes)
            {
                EmitProxy(classSpec);
            }
        }

        private void EmitExtensions()
        {
            var getProxyBody = new StringBuilder();

            foreach (var classSpec in _spec.Classes)
            {
                var fqIntfTypeName = classSpec.FullyQualifiedInterfaceTypeName;
                var fqClassTypeName =
                    $"{_spec.GetterNamespace}.{_spec.GetterClassName}.{classSpec.ClassTypeName}";
                getProxyBody.Append($@"
            if (typeof({_spec.GetterTypeParameterName}) == typeof({fqIntfTypeName}))
            {{
                return ({_spec.GetterTypeParameterName}) ({fqIntfTypeName}) new {fqClassTypeName}({_spec.GetterHubConnectionParameterName});
            }}");
            }

            var getProxy = GeneratorHelpers.SourceFilePrefix() + $@"
using Microsoft.AspNetCore.SignalR.Client;

namespace {_spec.GetterNamespace}
{{
    {_spec.GetterClassAccessibility} static partial class {_spec.GetterClassName}
    {{
        {_spec.GetterMethodAccessibility} static partial {_spec.GetterTypeParameterName} {_spec.GetterMethodName}<{_spec.GetterTypeParameterName}>(this HubConnection {_spec.GetterHubConnectionParameterName})
        {{
{getProxyBody.ToString()}
            throw new System.ArgumentException(nameof({_spec.GetterTypeParameterName}));
        }}
    }}
}}";

            _context.AddSource("HubServerProxy.g.cs", SourceText.From(getProxy.ToString(), Encoding.UTF8));
        }

        private void EmitProxy(ClassSpec classSpec)
        {
            var methods = new StringBuilder();

            foreach (var methodSpec in classSpec.Methods)
            {
                var signature = new StringBuilder($"public {methodSpec.FullyQualifiedReturnTypeName} {methodSpec.Name}(");
                var callArgs = new StringBuilder("");
                var signatureArgs = new StringBuilder("");
                var first = true;
                foreach (var argumentSpec in methodSpec.Arguments)
                {
                    if (!first)
                    {
                        signatureArgs.Append(", ");
                    }

                    first = false;
                    signatureArgs.Append($"{argumentSpec.FullyQualifiedTypeName} {argumentSpec.Name}");
                    callArgs.Append($", {argumentSpec.Name}");
                }
                signature.Append(signatureArgs);
                signature.Append(')');

                // Prepare method body
                string body;
                if (methodSpec.Support != SupportClassification.Supported)
                {
                    body = methodSpec.SupportHint is null
                        ? "throw new System.NotSupportedException();"
                        : $"throw new System.NotSupportedException(\"{methodSpec.SupportHint}\");";
                }
                else
                {
                    // Get specific hub connection extension method call
                    var specificCall = GetSpecificCall(methodSpec);

                    // Handle ValueTask
                    var prefix = "";
                    var suffix = "";
                    if (methodSpec.IsReturnTypeValueTask)
                    {
                        if (methodSpec.InnerReturnTypeName is not null)
                        {
                            prefix = $"new System.Threading.Tasks.ValueTask<{methodSpec.InnerReturnTypeName}>(";
                        }
                        else
                        {
                            prefix = "new System.Threading.Tasks.ValueTask(";
                        }
                        suffix = $")";
                    }

                    // Bake it all together
                    body = $"return {prefix}this.connection.{specificCall}(\"{methodSpec.Name}\"{callArgs}){suffix};";
                }

                var method = $@"
        {signature}
        {{
            {body}
        }}
";
                methods.Append(method);
            }

            var proxy = GeneratorHelpers.SourceFilePrefix() + $@"
using Microsoft.AspNetCore.SignalR.Client;

namespace {_spec.GetterNamespace}
{{
    {_spec.GetterClassAccessibility} static partial class {_spec.GetterClassName}
    {{
        private sealed class {classSpec.ClassTypeName} : {classSpec.FullyQualifiedInterfaceTypeName}
        {{
            private readonly HubConnection connection;
            internal {classSpec.ClassTypeName}(HubConnection connection)
            {{
                this.connection = connection;
            }}
    {methods.ToString()}
        }}
    }}
}}";

            _context.AddSource($"HubServerProxy.{classSpec.ClassTypeName}.g.cs", SourceText.From(proxy.ToString(), Encoding.UTF8));
        }

        private static string GetSpecificCall(MethodSpec methodSpec)
        {
            if (methodSpec.Stream.HasFlag(StreamSpec.ServerToClient) &&
                !methodSpec.Stream.HasFlag(StreamSpec.AsyncEnumerable))
            {
                return $"StreamAsChannelAsync<{methodSpec.InnerReturnTypeName}>";
            }

            if (methodSpec.Stream.HasFlag(StreamSpec.ServerToClient) &&
                methodSpec.Stream.HasFlag(StreamSpec.AsyncEnumerable))
            {
                return $"StreamAsync<{methodSpec.InnerReturnTypeName}>";
            }

            if (methodSpec.InnerReturnTypeName is not null)
            {
                return $"InvokeAsync<{methodSpec.InnerReturnTypeName}>";
            }

            if (methodSpec.Stream.HasFlag(StreamSpec.ClientToServer))
            {
                return "SendAsync";
            }

            return "InvokeAsync";
        }
    }
}
