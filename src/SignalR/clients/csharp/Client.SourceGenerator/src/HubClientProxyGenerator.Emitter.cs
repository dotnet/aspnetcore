// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.SignalR.Client.SourceGenerator;

internal partial class HubClientProxyGenerator
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
            if (string.IsNullOrEmpty(_spec.SetterClassAccessibility) ||
                string.IsNullOrEmpty(_spec.SetterMethodAccessibility) ||
                string.IsNullOrEmpty(_spec.SetterClassName) ||
                string.IsNullOrEmpty(_spec.SetterMethodName) ||
                string.IsNullOrEmpty(_spec.SetterTypeParameterName) ||
                string.IsNullOrEmpty(_spec.SetterHubConnectionParameterName))
            {
                return;
            }

            // Generate extensions and other user facing mostly-static code in a single source file
            EmitExtensions();
            // Generate specific callback registration methods in their own source file for each provider type
            foreach (var typeSpec in _spec.Types)
            {
                EmitRegistrationMethod(typeSpec);
            }
        }

        private void EmitExtensions()
        {
            var registerProviderBody = new StringBuilder();

            // Generate body of RegisterCallbackProvider<T>
            foreach (var typeSpec in _spec.Types)
            {
                var methodName = $"Register{typeSpec.FullyQualifiedTypeName.Replace(".", string.Empty)}";
                var fqtn = typeSpec.FullyQualifiedTypeName;
                registerProviderBody.AppendLine($@"
            if (typeof({_spec.SetterTypeParameterName}) == typeof({fqtn}))
            {{
                return (System.IDisposable) new CallbackProviderRegistration({methodName}({_spec.SetterHubConnectionParameterName}, ({fqtn}) {_spec.SetterProviderParameterName}));
            }}");
            }

            // Generate RegisterCallbackProvider<T> extension method and CallbackProviderRegistration class
            // RegisterCallbackProvider<T> is used by end-user to register their callback provider types
            // CallbackProviderRegistration is a private implementation of IDisposable which simply holds
            //  an array of IDisposables acquired from registration of each callback method from HubConnection
            var extensions = GeneratorHelpers.SourceFilePrefix() + $@"
using Microsoft.AspNetCore.SignalR.Client;

namespace {_spec.SetterNamespace}
{{
    {_spec.SetterClassAccessibility} static partial class {_spec.SetterClassName}
    {{
        {_spec.SetterMethodAccessibility} static partial System.IDisposable {_spec.SetterMethodName}<{_spec.SetterTypeParameterName}>(this HubConnection {_spec.SetterHubConnectionParameterName}, {_spec.SetterTypeParameterName} {_spec.SetterProviderParameterName})
        {{
            if ({_spec.SetterProviderParameterName} is null)
            {{
                throw new System.ArgumentNullException(""{_spec.SetterProviderParameterName}"");
            }}
{registerProviderBody.ToString()}
            throw new System.ArgumentException(nameof({_spec.SetterTypeParameterName}));
        }}

        private sealed class CallbackProviderRegistration : System.IDisposable
        {{
            private System.IDisposable[]? registrations;
            public CallbackProviderRegistration(params System.IDisposable[] registrations)
            {{
                this.registrations = registrations;
            }}

            public void Dispose()
            {{
                if (this.registrations is null)
                {{
                    return;
                }}

                System.Collections.Generic.List<System.Exception>? exceptions = null;
                foreach(var registration in this.registrations)
                {{
                    try
                    {{
                        registration.Dispose();
                    }}
                    catch (System.Exception exc)
                    {{
                        if (exceptions is null)
                        {{
                            exceptions = new ();
                        }}

                        exceptions.Add(exc);
                    }}
                }}
                this.registrations = null;
                if (exceptions is not null)
                {{
                    throw new System.AggregateException(exceptions);
                }}
            }}
        }}
    }}
}}";

            _context.AddSource("HubClientProxy.g.cs", SourceText.From(extensions.ToString(), Encoding.UTF8));
        }

        private void EmitRegistrationMethod(TypeSpec typeSpec)
        {
            // The actual registration method goes thru each method that the callback provider type has and then
            //  registers the method with HubConnection and stashes the returned IDisposable into an array for
            //  later consumption by CallbackProviderRegistration's constructor
            var registrationMethodBody = new StringBuilder(GeneratorHelpers.SourceFilePrefix() + $@"
using Microsoft.AspNetCore.SignalR.Client;

namespace {_spec.SetterNamespace}
{{
    {_spec.SetterClassAccessibility} static partial class {_spec.SetterClassName}
    {{
        private static System.IDisposable[] Register{typeSpec.FullyQualifiedTypeName.Replace(".", string.Empty)}(HubConnection connection, {typeSpec.FullyQualifiedTypeName} provider)
        {{
            var registrations = new System.IDisposable[{typeSpec.Methods.Count}];");

            // Generate each of the methods
            var i = 0;
            foreach (var member in typeSpec.Methods)
            {
                var genericArgs = new StringBuilder();
                var lambaParams = new StringBuilder();

                // Populate call with its parameters
                var first = true;
                foreach (var parameter in member.Arguments)
                {
                    if (first)
                    {
                        genericArgs.Append('<');
                        lambaParams.Append('(');
                    }
                    else
                    {
                        genericArgs.Append(", ");
                        lambaParams.Append(", ");
                    }

                    first = false;
                    genericArgs.Append($"{parameter.FullyQualifiedTypeName}");
                    lambaParams.Append($"{parameter.Name}");
                }

                if (!first)
                {
                    genericArgs.Append('>');
                    lambaParams.Append(')');
                }
                else
                {
                    lambaParams.Append("()");
                }

                var lambda = $"{lambaParams} => provider.{member.Name}{lambaParams}";
                var call = $"connection.On{genericArgs}(\"{member.Name}\", {lambda})";

                registrationMethodBody.AppendLine($@"
            registrations[{i}] = {call};");
                ++i;
            }
            registrationMethodBody.AppendLine(@"
            return registrations;
        }
    }
}");

            _context.AddSource($"HubClientProxy.{typeSpec.TypeName}.g.cs", SourceText.From(registrationMethodBody.ToString(), Encoding.UTF8));
        }
    }
}
