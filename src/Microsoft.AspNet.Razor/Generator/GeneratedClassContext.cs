// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Internal.Web.Utils;

namespace Microsoft.AspNet.Razor.Generator
{
    public struct GeneratedClassContext
    {
        public static readonly string DefaultWriteMethodName = "Write";
        public static readonly string DefaultWriteLiteralMethodName = "WriteLiteral";
        public static readonly string DefaultExecuteMethodName = "ExecuteAsync";
        public static readonly string DefaultLayoutPropertyName = "Layout";
        public static readonly string DefaultWriteAttributeMethodName = "WriteAttribute";
        public static readonly string DefaultWriteAttributeToMethodName = "WriteAttributeTo";

        public static readonly GeneratedClassContext Default = new GeneratedClassContext(DefaultExecuteMethodName,
                                                                                         DefaultWriteMethodName,
                                                                                         DefaultWriteLiteralMethodName);

        public GeneratedClassContext(string executeMethodName, string writeMethodName, string writeLiteralMethodName)
            : this()
        {
            if (String.IsNullOrEmpty(executeMethodName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                                          CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                                                          "executeMethodName"),
                                            "executeMethodName");
            }
            if (String.IsNullOrEmpty(writeMethodName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                                          CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                                                          "writeMethodName"),
                                            "writeMethodName");
            }
            if (String.IsNullOrEmpty(writeLiteralMethodName))
            {
                throw new ArgumentException(String.Format(CultureInfo.CurrentCulture,
                                                          CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                                                          "writeLiteralMethodName"),
                                            "writeLiteralMethodName");
            }

            WriteMethodName = writeMethodName;
            WriteLiteralMethodName = writeLiteralMethodName;
            ExecuteMethodName = executeMethodName;

            WriteToMethodName = null;
            WriteLiteralToMethodName = null;
            TemplateTypeName = null;
            DefineSectionMethodName = null;

            LayoutPropertyName = DefaultLayoutPropertyName;
            WriteAttributeMethodName = DefaultWriteAttributeMethodName;
            WriteAttributeToMethodName = DefaultWriteAttributeToMethodName;
        }

        public GeneratedClassContext(string executeMethodName,
                                     string writeMethodName,
                                     string writeLiteralMethodName,
                                     string writeToMethodName,
                                     string writeLiteralToMethodName,
                                     string templateTypeName)
            : this(executeMethodName, writeMethodName, writeLiteralMethodName)
        {
            WriteToMethodName = writeToMethodName;
            WriteLiteralToMethodName = writeLiteralToMethodName;
            TemplateTypeName = templateTypeName;
        }

        public GeneratedClassContext(string executeMethodName,
                                     string writeMethodName,
                                     string writeLiteralMethodName,
                                     string writeToMethodName,
                                     string writeLiteralToMethodName,
                                     string templateTypeName,
                                     string defineSectionMethodName)
            : this(executeMethodName, writeMethodName, writeLiteralMethodName, writeToMethodName, writeLiteralToMethodName, templateTypeName)
        {
            DefineSectionMethodName = defineSectionMethodName;
        }

        public GeneratedClassContext(string executeMethodName,
                                     string writeMethodName,
                                     string writeLiteralMethodName,
                                     string writeToMethodName,
                                     string writeLiteralToMethodName,
                                     string templateTypeName,
                                     string defineSectionMethodName,
                                     string beginContextMethodName,
                                     string endContextMethodName)
            : this(executeMethodName, writeMethodName, writeLiteralMethodName, writeToMethodName, writeLiteralToMethodName, templateTypeName, defineSectionMethodName)
        {
            BeginContextMethodName = beginContextMethodName;
            EndContextMethodName = endContextMethodName;
        }

        public string WriteMethodName { get; private set; }
        public string WriteLiteralMethodName { get; private set; }
        public string WriteToMethodName { get; private set; }
        public string WriteLiteralToMethodName { get; private set; }
        public string ExecuteMethodName { get; private set; }

        // Optional Items
        public string BeginContextMethodName { get; set; }
        public string EndContextMethodName { get; set; }
        public string LayoutPropertyName { get; set; }
        public string DefineSectionMethodName { get; set; }
        public string TemplateTypeName { get; set; }
        public string WriteAttributeMethodName { get; set; }
        public string WriteAttributeToMethodName { get; set; }

        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Property is not a URL property")]
        public string ResolveUrlMethodName { get; set; }

        public bool AllowSections
        {
            get { return !String.IsNullOrEmpty(DefineSectionMethodName); }
        }

        public bool AllowTemplates
        {
            get { return !String.IsNullOrEmpty(TemplateTypeName); }
        }

        public bool SupportsInstrumentation
        {
            get { return !String.IsNullOrEmpty(BeginContextMethodName) && !String.IsNullOrEmpty(EndContextMethodName); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GeneratedClassContext))
            {
                return false;
            }
            GeneratedClassContext other = (GeneratedClassContext)obj;
            return String.Equals(DefineSectionMethodName, other.DefineSectionMethodName, StringComparison.Ordinal) &&
                   String.Equals(WriteMethodName, other.WriteMethodName, StringComparison.Ordinal) &&
                   String.Equals(WriteLiteralMethodName, other.WriteLiteralMethodName, StringComparison.Ordinal) &&
                   String.Equals(WriteToMethodName, other.WriteToMethodName, StringComparison.Ordinal) &&
                   String.Equals(WriteLiteralToMethodName, other.WriteLiteralToMethodName, StringComparison.Ordinal) &&
                   String.Equals(ExecuteMethodName, other.ExecuteMethodName, StringComparison.Ordinal) &&
                   String.Equals(TemplateTypeName, other.TemplateTypeName, StringComparison.Ordinal) &&
                   String.Equals(BeginContextMethodName, other.BeginContextMethodName, StringComparison.Ordinal) &&
                   String.Equals(EndContextMethodName, other.EndContextMethodName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            // TODO: Use HashCodeCombiner
            return DefineSectionMethodName.GetHashCode() ^
                   WriteMethodName.GetHashCode() ^
                   WriteLiteralMethodName.GetHashCode() ^
                   WriteToMethodName.GetHashCode() ^
                   WriteLiteralToMethodName.GetHashCode() ^
                   ExecuteMethodName.GetHashCode() ^
                   TemplateTypeName.GetHashCode() ^
                   BeginContextMethodName.GetHashCode() ^
                   EndContextMethodName.GetHashCode();
        }

        public static bool operator ==(GeneratedClassContext left, GeneratedClassContext right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GeneratedClassContext left, GeneratedClassContext right)
        {
            return !left.Equals(right);
        }
    }
}
