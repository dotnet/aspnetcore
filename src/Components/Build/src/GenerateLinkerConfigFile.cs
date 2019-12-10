// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Build
{
    public class GenerateLinkerConfigFile : Task
    {
        const string ComponentsAssemblyName = "Microsoft.AspNetCore.Components";
        const string JSInteropAssemblyName = "Microsoft.JSInterop";
        const string ComponentInterfaceName = "Microsoft.AspNetCore.Components.IComponent";
        const string JSInvokableAttributeName = "Microsoft.JSInterop.JSInvokableAttribute";

        [Required]
        public string AssemblyPath { get; set; }

        public override bool Execute()
        {
            var assembly = Assembly.LoadFrom(AssemblyPath);
            using (var outputStream = new FileStream(
                Path.ChangeExtension(AssemblyPath, ".linkerconfig.xml"),
                FileMode.Create))
            using (var xmlWriter = XmlWriter.Create(outputStream, new XmlWriterSettings { Indent = true }))
            {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("linker");
                xmlWriter.WriteStartElement("assembly");
                xmlWriter.WriteAttributeString("fullname", assembly.GetName().Name);

                // Preserve component types
                var componentTypes = GetComponentTypes(assembly).ToList();
                if (componentTypes.Any())
                {
                    xmlWriter.WriteComment("Component must be preserved in full, otherwise their constructors and parameter properties will be removed");
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
                    xmlWriter.WriteComment("JSInterop-callable methods are only called through reflection");
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

                xmlWriter.WriteEndElement(); // assembly
                xmlWriter.WriteEndElement(); // linker
                xmlWriter.WriteEndDocument();
            }

            return true;
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
