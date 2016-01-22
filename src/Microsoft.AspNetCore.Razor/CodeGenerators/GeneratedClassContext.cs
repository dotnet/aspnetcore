// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.CodeGenerators
{
    public struct GeneratedClassContext
    {
        public static readonly string DefaultWriteMethodName = "Write";
        public static readonly string DefaultWriteLiteralMethodName = "WriteLiteral";
        public static readonly string DefaultExecuteMethodName = "ExecuteAsync";
        public static readonly string DefaultBeginWriteAttributeMethodName = "BeginWriteAttribute";
        public static readonly string DefaultBeginWriteAttributeToMethodName = "BeginWriteAttributeTo";
        public static readonly string DefaultEndWriteAttributeMethodName = "EndWriteAttribute";
        public static readonly string DefaultEndWriteAttributeToMethodName = "EndWriteAttributeTo";
        public static readonly string DefaultWriteAttributeValueMethodName = "WriteAttributeValue";
        public static readonly string DefaultWriteAttributeValueToMethodName = "WriteAttributeValueTo";

        public static readonly GeneratedClassContext Default =
            new GeneratedClassContext(
                DefaultExecuteMethodName,
                DefaultWriteMethodName,
                DefaultWriteLiteralMethodName,
                new GeneratedTagHelperContext());

        public GeneratedClassContext(
            string executeMethodName,
            string writeMethodName,
            string writeLiteralMethodName,
            GeneratedTagHelperContext generatedTagHelperContext)
            : this()
        {
            if (generatedTagHelperContext == null)
            {
                throw new ArgumentNullException(nameof(generatedTagHelperContext));
            }

            if (string.IsNullOrEmpty(executeMethodName))
            {
                throw new ArgumentException(
                    CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                    nameof(executeMethodName));
            }
            if (string.IsNullOrEmpty(writeMethodName))
            {
                throw new ArgumentException(
                    CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                    nameof(writeMethodName));
            }
            if (string.IsNullOrEmpty(writeLiteralMethodName))
            {
                throw new ArgumentException(
                    CommonResources.Argument_Cannot_Be_Null_Or_Empty,
                    nameof(writeLiteralMethodName));
            }

            GeneratedTagHelperContext = generatedTagHelperContext;

            WriteMethodName = writeMethodName;
            WriteLiteralMethodName = writeLiteralMethodName;
            ExecuteMethodName = executeMethodName;

            WriteToMethodName = null;
            WriteLiteralToMethodName = null;
            TemplateTypeName = null;
            DefineSectionMethodName = null;

            BeginWriteAttributeMethodName = DefaultBeginWriteAttributeMethodName;
            BeginWriteAttributeToMethodName = DefaultBeginWriteAttributeToMethodName;
            EndWriteAttributeMethodName = DefaultEndWriteAttributeMethodName;
            EndWriteAttributeToMethodName = DefaultEndWriteAttributeToMethodName;
            WriteAttributeValueMethodName = DefaultWriteAttributeValueMethodName;
            WriteAttributeValueToMethodName = DefaultWriteAttributeValueToMethodName;
        }

        public GeneratedClassContext(
            string executeMethodName,
            string writeMethodName,
            string writeLiteralMethodName,
            string writeToMethodName,
            string writeLiteralToMethodName,
            string templateTypeName,
            GeneratedTagHelperContext generatedTagHelperContext)
            : this(executeMethodName,
                   writeMethodName,
                   writeLiteralMethodName,
                   generatedTagHelperContext)
        {
            WriteToMethodName = writeToMethodName;
            WriteLiteralToMethodName = writeLiteralToMethodName;
            TemplateTypeName = templateTypeName;
        }

        public GeneratedClassContext(
            string executeMethodName,
            string writeMethodName,
            string writeLiteralMethodName,
            string writeToMethodName,
            string writeLiteralToMethodName,
            string templateTypeName,
            string defineSectionMethodName,
            GeneratedTagHelperContext generatedTagHelperContext)
            : this(executeMethodName,
                   writeMethodName,
                   writeLiteralMethodName,
                   writeToMethodName,
                   writeLiteralToMethodName,
                   templateTypeName,
                   generatedTagHelperContext)
        {
            DefineSectionMethodName = defineSectionMethodName;
        }

        public GeneratedClassContext(
            string executeMethodName,
            string writeMethodName,
            string writeLiteralMethodName,
            string writeToMethodName,
            string writeLiteralToMethodName,
            string templateTypeName,
            string defineSectionMethodName,
            string beginContextMethodName,
            string endContextMethodName,
            GeneratedTagHelperContext generatedTagHelperContext)
            : this(executeMethodName,
                   writeMethodName,
                   writeLiteralMethodName,
                   writeToMethodName,
                   writeLiteralToMethodName,
                   templateTypeName,
                   defineSectionMethodName,
                   generatedTagHelperContext)
        {
            BeginContextMethodName = beginContextMethodName;
            EndContextMethodName = endContextMethodName;
        }

        // Required Items
        public string WriteMethodName { get; }
        public string WriteLiteralMethodName { get; }
        public string WriteToMethodName { get; }
        public string WriteLiteralToMethodName { get; }
        public string ExecuteMethodName { get; }
        public GeneratedTagHelperContext GeneratedTagHelperContext { get; }

        // Optional Items
        public string BeginContextMethodName { get; set; }
        public string EndContextMethodName { get; set; }
        public string DefineSectionMethodName { get; set; }
        public string TemplateTypeName { get; set; }

        public string BeginWriteAttributeMethodName { get; set; }
        public string BeginWriteAttributeToMethodName { get; set; }
        public string EndWriteAttributeMethodName { get; set; }
        public string EndWriteAttributeToMethodName { get; set; }
        public string WriteAttributeValueMethodName { get; set; }
        public string WriteAttributeValueToMethodName { get; set; }

        public bool AllowSections
        {
            get { return !string.IsNullOrEmpty(DefineSectionMethodName); }
        }

        public bool AllowTemplates
        {
            get { return !string.IsNullOrEmpty(TemplateTypeName); }
        }

        public bool SupportsInstrumentation
        {
            get { return !string.IsNullOrEmpty(BeginContextMethodName) && !string.IsNullOrEmpty(EndContextMethodName); }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GeneratedClassContext))
            {
                return false;
            }

            var other = (GeneratedClassContext)obj;
            return string.Equals(DefineSectionMethodName, other.DefineSectionMethodName, StringComparison.Ordinal) &&
                string.Equals(WriteMethodName, other.WriteMethodName, StringComparison.Ordinal) &&
                string.Equals(WriteLiteralMethodName, other.WriteLiteralMethodName, StringComparison.Ordinal) &&
                string.Equals(WriteToMethodName, other.WriteToMethodName, StringComparison.Ordinal) &&
                string.Equals(WriteLiteralToMethodName, other.WriteLiteralToMethodName, StringComparison.Ordinal) &&
                string.Equals(ExecuteMethodName, other.ExecuteMethodName, StringComparison.Ordinal) &&
                string.Equals(TemplateTypeName, other.TemplateTypeName, StringComparison.Ordinal) &&
                string.Equals(BeginContextMethodName, other.BeginContextMethodName, StringComparison.Ordinal) &&
                string.Equals(EndContextMethodName, other.EndContextMethodName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            // Hash code should include only immutable properties.
            var hashCodeCombiner = HashCodeCombiner.Start();

            hashCodeCombiner.Add(WriteMethodName, StringComparer.Ordinal);
            hashCodeCombiner.Add(WriteLiteralMethodName, StringComparer.Ordinal);
            hashCodeCombiner.Add(WriteToMethodName, StringComparer.Ordinal);
            hashCodeCombiner.Add(WriteLiteralToMethodName, StringComparer.Ordinal);
            hashCodeCombiner.Add(ExecuteMethodName, StringComparer.Ordinal);

            return hashCodeCombiner;
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
