// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Mono.Cecil;

namespace Microsoft.AspNetCore.Components.Build
{
    internal static class LinkerConfigGenerator
    {
        private const string ComponentInterfaceName = "Microsoft.AspNetCore.Components.IComponent";
        private const string JSInvokableAttributeName = "Microsoft.JSInterop.JSInvokableAttribute";

        public static void Generate(string assemblyPath, Stream outputStream)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters
            {
                AssemblyResolver = assemblyResolver
            });
            var module = assemblyDefinition.MainModule;

            var writerSettings = new XmlWriterSettings
            {
                Indent = true,
                Encoding = new UTF8Encoding(false) // No BOM
            };

            using (var xmlWriter = XmlWriter.Create(outputStream, writerSettings))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(" THIS IS A GENERATED FILE - DO NOT EDIT MANUALLY ");

                xmlWriter.WriteStartElement("linker");
                xmlWriter.WriteStartElement("assembly");
                xmlWriter.WriteAttributeString("fullname", assemblyDefinition.Name.Name);

                // Preserve component types
                var componentTypes = GetComponentTypes(module).ToList();
                if (componentTypes.Any())
                {
                    xmlWriter.WriteComment(" Components must be preserved in full, otherwise their constructors and parameter properties will be removed ");
                    foreach (var componentType in componentTypes)
                    {
                        xmlWriter.WriteStartElement("type");
                        xmlWriter.WriteAttributeString("fullname", componentType.FullName);
                        xmlWriter.WriteEndElement();
                    }
                }

                // Preserve JSInterop-callable methods
                var jsInteropMethods = GetJSInteropMethods(module).GroupBy(m => m.DeclaringType).ToList();
                if (jsInteropMethods.Any())
                {
                    xmlWriter.WriteComment(" JSInterop-callable methods are only called through reflection ");
                    foreach (var group in jsInteropMethods)
                    {
                        xmlWriter.WriteStartElement("type");
                        xmlWriter.WriteAttributeString("fullname", group.Key.FullName);
                        foreach (var method in group)
                        {
                            xmlWriter.WriteStartElement("method");
                            xmlWriter.WriteAttributeString("signature", GetMethodSignature(method));
                            xmlWriter.WriteEndElement(); // method
                        }
                        xmlWriter.WriteEndElement(); // type
                    }
                }

                xmlWriter.WriteEndElement(); // assembly
                xmlWriter.WriteEndElement(); // linker
                xmlWriter.WriteEndDocument();
            }
        }

        private static IEnumerable<TypeDefinition> GetComponentTypes(ModuleDefinition module)
        {
            foreach (var typeDefinition in module.Types)
            {
                foreach (var @interface in InterfacesIncludingInherited(typeDefinition, skipUnresolvable: true))
                {
                    if (@interface.InterfaceType.FullName.Equals(ComponentInterfaceName, StringComparison.Ordinal))
                    {
                        yield return typeDefinition;
                    }
                }
            }
        }

        private static IEnumerable<InterfaceImplementation> InterfacesIncludingInherited(this TypeDefinition typeDefinition, bool skipUnresolvable)
            => typeDefinition.BaseClasses(skipUnresolvable).SelectMany(c => c.Interfaces);

        public static IEnumerable<TypeDefinition> BaseClasses(this TypeDefinition typeDefinition, bool skipUnresolvable)
        {
            while (typeDefinition != null)
            {
                yield return typeDefinition;

                try
                {
                    typeDefinition = typeDefinition.BaseType?.Resolve();
                }
                catch (AssemblyResolutionException)
                {
                    if (skipUnresolvable)
                    {
                        typeDefinition = null;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private static IEnumerable<MethodDefinition> GetJSInteropMethods(ModuleDefinition module)
        {
            foreach (var typeDefinition in module.Types)
            {
                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    foreach (var customAttribute in methodDefinition.CustomAttributes)
                    {
                        if (customAttribute.AttributeType.FullName.Equals(JSInvokableAttributeName, StringComparison.Ordinal))
                        {
                            yield return methodDefinition;
                        }
                    }
                }
            }
        }

        // The output from this must correspond exactly to https://github.com/mono/linker/blob/master/src/linker/Linker.Steps/ResolveFromXmlStep.cs#L471
        // which is the whole reason we're using Cecil here. This isn't going to be maintainable in the long run, so we
        // either need the linker to be a regular NuGet package so we can reference its signature-generation code properly,
        // or we need some other way to indicate that methods with [JSInvokable] should always be preserved.
        //
        // The following method is shared source. Do not change the code style to match ASP.NET Core repo conventions,
        // because we want to be able to paste updated versions from the Mono repo and reason about the diff.
        static string GetMethodSignature(MethodDefinition meth)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(meth.ReturnType.FullName);
            sb.Append(" ");
            sb.Append(meth.Name);
            sb.Append("(");
            if (meth.HasParameters)
            {
                for (int i = 0; i < meth.Parameters.Count; i++)
                {
                    if (i > 0)
                        sb.Append(",");

                    sb.Append(meth.Parameters[i].ParameterType.FullName);
                }
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
