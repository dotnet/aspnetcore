// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml;

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
            using var assemblyReadStream = File.OpenRead(assemblyPath);
            using var peReader = new PEReader(assemblyReadStream);
            var metadata = peReader.GetMetadataReader();

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
                xmlWriter.WriteAttributeString("fullname", metadata.GetString(metadata.GetAssemblyDefinition().Name));

                /*
                // Preserve component types
                var componentTypes = GetComponentTypes(assembly).ToList();
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
                var jsInteropMethods = GetJSInteropMethods(assembly).GroupBy(m => m.DeclaringType).ToList();
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
                            xmlWriter.WriteAttributeString("signature", ToSignatureString(method));
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

        private static string ToSignatureString(MethodInfo method)
        {
            // This produces a result that slightly differs from Mono linker docs
            // For example, it represents void as "Void", whereas Mono linker docs say "System.Void"
            return method.ToString();
        }

        private static IEnumerable<Type> GetComponentTypes(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var @interface in type.GetInterfaces())
                {
                    if (@interface.Assembly.GetName().Name.Equals(ComponentsAssemblyName, StringComparison.OrdinalIgnoreCase)
                        && @interface.FullName.Equals(ComponentInterfaceName, StringComparison.OrdinalIgnoreCase))
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<MethodInfo> GetJSInteropMethods(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    foreach (var attribute in method.GetCustomAttributes(true))
                    {
                        if (attribute.GetType().Assembly.GetName().Name.Equals(JSInteropAssemblyName, StringComparison.OrdinalIgnoreCase)
                            && attribute.GetType().FullName.Equals(JSInvokableAttributeName, StringComparison.OrdinalIgnoreCase))
                        {
                            yield return method;
                        }
                    }
                }
            }
        }
    }
}
