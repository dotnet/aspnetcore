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
        const string ComponentsAssemblyName = "Microsoft.AspNetCore.Components";
        const string JSInteropAssemblyName = "Microsoft.JSInterop";
        const string ComponentInterfaceName = "Microsoft.AspNetCore.Components.IComponent";
        const string JSInvokableAttributeName = "Microsoft.JSInterop.JSInvokableAttribute";

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

                /*
                // Preserve JSInterop-callable methods
                var jsInteropMethods = GetJSInteropMethods(metadata).GroupBy(m => m.GetDeclaringType()).ToList();
                if (jsInteropMethods.Any())
                {
                    xmlWriter.WriteComment(" JSInterop-callable methods are only called through reflection ");
                    foreach (var group in jsInteropMethods)
                    {
                        xmlWriter.WriteStartElement("type");
                        xmlWriter.WriteAttributeString("fullname", FullyQualifiedName(metadata, group.Key));
                        foreach (var method in group)
                        {
                            xmlWriter.WriteStartElement("method");
                            xmlWriter.WriteAttributeString("signature", ToSignatureString(metadata, method));
                            xmlWriter.WriteEndElement(); // method
                        }
                        xmlWriter.WriteEndElement(); // type
                    }
                }
                */

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

        /*
        private static string FullyQualifiedName(MetadataReader metadata, EntityHandle entityHandle)
        {
            if (entityHandle.IsNil)
            {
                return "NIL";
            }

            switch (entityHandle.Kind)
            {
                case HandleKind.TypeDefinition:
                    var typeDefinitionHandle = (TypeDefinitionHandle)entityHandle;
                    var typeDefinition = metadata.GetTypeDefinition(typeDefinitionHandle);
                    return $"{metadata.GetString(typeDefinition.Namespace)}.{metadata.GetString(typeDefinition.Name)}";
                case HandleKind.TypeReference:
                    var typeReferenceHandle = (TypeReferenceHandle)entityHandle;
                    var typeReference = metadata.GetTypeReference(typeReferenceHandle);
                    return $"{metadata.GetString(typeReference.Namespace)}.{metadata.GetString(typeReference.Name)}";
                default:
                    return $"[Unsupported handle kind: {entityHandle.Kind}]";
            }           
        }

        private static IEnumerable<MethodDefinition> GetJSInteropMethods(MetadataReader metadata)
        {
            foreach (var methodDefinition in metadata.MethodDefinitions.Select(m => metadata.GetMethodDefinition(m)))
            {
                foreach (var customAttribute in methodDefinition.GetCustomAttributes().Select(a => metadata.GetCustomAttribute(a)))
                {
                    var ctor = customAttribute.Constructor;
                    if (ctor.Kind == HandleKind.MemberReference)
                    {
                        var ctorMethod = metadata.GetMemberReference((MemberReferenceHandle)ctor);
                        var ctorTypeHandle = ctorMethod.Parent;
                        if (FullyQualifiedName(metadata, ctorTypeHandle).Equals(JSInvokableAttributeName, StringComparison.Ordinal))
                        {
                            yield return methodDefinition;
                        }
                    }
                }
            }
        }
        */
    }
}
