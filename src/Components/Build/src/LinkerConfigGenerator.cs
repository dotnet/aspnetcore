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
        private const string LinkerPreserveAttributeName = "Microsoft.AspNetCore.Components.LinkerPreserveAttribute";
        private readonly static string EventArgsTypeName = typeof(EventArgs).FullName;

        public static string Generate(string assemblyPath)
        {
            var assemblyResolver = new DefaultAssemblyResolver();
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));
            var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters
            {
                AssemblyResolver = assemblyResolver
            });
            var module = assemblyDefinition.MainModule;

            using (var memoryStream = new MemoryStream()) // Avoiding StringWriter so we get UTF-8 output
            using (var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteComment(" THIS IS A GENERATED FILE - DO NOT EDIT MANUALLY ");

                xmlWriter.WriteStartElement("linker");
                xmlWriter.WriteStartElement("assembly");
                xmlWriter.WriteAttributeString("fullname", assemblyDefinition.Name.Name);

                PreserveTypes(
                    xmlWriter,
                    GetComponentTypes(module).ToList(),
                    "Components must be preserved in full, otherwise their constructors and parameter properties will be removed");

                PreserveTypes(
                    xmlWriter,
                    GetEventArgsTypes(module).ToList(),
                    "EventArgs subclasses must be preserved in full, as their property setters are only called through JSON deserialization");

                PreserveMethods(
                    xmlWriter,
                    GetJSInteropMethods(module).ToList(),
                    "JSInterop-callable methods are only called through reflection");

                PreserveTypes(
                    xmlWriter,
                    GetLinkerPreserveTypes(module).ToList(),
                    "Types with [LinkerPreserve]");

                xmlWriter.WriteEndElement(); // assembly
                xmlWriter.WriteEndElement(); // linker
                xmlWriter.WriteEndDocument();
                xmlWriter.Flush();

                memoryStream.Seek(0, SeekOrigin.Begin);
                return new StreamReader(memoryStream).ReadToEnd();
            }
        }

        private static void PreserveMethods(XmlWriter xmlWriter, IReadOnlyCollection<MethodDefinition> methods, string comment)
        {
            if (methods.Any())
            {
                xmlWriter.WriteComment($" {comment} ");
                foreach (var group in methods.GroupBy(m => m.DeclaringType))
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
        }

        private static void PreserveTypes(XmlWriter writer, IReadOnlyCollection<TypeDefinition> types, string comment)
        {
            if (types.Any())
            {
                writer.WriteComment($" {comment} ");
                foreach (var componentType in types)
                {
                    writer.WriteStartElement("type");
                    writer.WriteAttributeString("fullname", componentType.FullName);
                    writer.WriteEndElement();
                }
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

        private static IEnumerable<TypeDefinition> GetEventArgsTypes(ModuleDefinition module)
        {
            return module.Types.Where(t => BaseClasses(t, skipUnresolvable: true).Any(
                baseClass => baseClass.FullName.Equals(EventArgsTypeName, StringComparison.Ordinal)));
        }

        private static IEnumerable<TypeDefinition> GetLinkerPreserveTypes(ModuleDefinition module)
        {
            return module.Types.Where(t => t.CustomAttributes.Any(
                a => a.AttributeType.FullName.Equals(LinkerPreserveAttributeName, StringComparison.Ordinal)));
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
