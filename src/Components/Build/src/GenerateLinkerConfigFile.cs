// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.AspNetCore.Components.Build
{
    public class GenerateLinkerConfigFile : Task
    {
        const string ComponentsAssemblyName = "Microsoft.AspNetCore.Components";
        const string ComponentInterfaceName = "Microsoft.AspNetCore.Components.IComponent";

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
                xmlWriter.WriteComment("Component must be preserved in full, otherwise their constructors and parameter properties will be removed");
                foreach (var componentType in GetComponentTypes(assembly))
                {
                    xmlWriter.WriteStartElement("type");
                    xmlWriter.WriteAttributeString("fullname", componentType.FullName);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement(); // assembly
                xmlWriter.WriteEndElement(); // linker
                xmlWriter.WriteEndDocument();
            }

            return true;
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
    }
}
