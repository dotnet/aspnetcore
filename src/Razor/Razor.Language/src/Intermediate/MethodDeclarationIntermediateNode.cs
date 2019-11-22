// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate
{
    public sealed class MethodDeclarationIntermediateNode : MemberDeclarationIntermediateNode
    {
        public override IntermediateNodeCollection Children { get; } = new IntermediateNodeCollection();

        public IList<string> Modifiers { get; } = new List<string>();

        public string MethodName { get; set; }

        public IList<MethodParameter> Parameters { get; } = new List<MethodParameter>();

        public string ReturnType { get; set; }

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            if (visitor == null)
            {
                throw new ArgumentNullException(nameof(visitor));
            }

            visitor.VisitMethodDeclaration(this);
        }

        public override void FormatNode(IntermediateNodeFormatter formatter)
        {
            formatter.WriteContent(MethodName);

            formatter.WriteProperty(nameof(MethodName), MethodName);
            formatter.WriteProperty(nameof(Modifiers), string.Join(", ", Modifiers));
            formatter.WriteProperty(nameof(Parameters), string.Join(", ", Parameters.Select(FormatMethodParameter)));
            formatter.WriteProperty(nameof(ReturnType), ReturnType);
        }

        private static string FormatMethodParameter(MethodParameter parameter)
        {
            var builder = new StringBuilder();
            for (var i = 0; i <parameter.Modifiers.Count; i++)
            {
                builder.Append(parameter.Modifiers[i]);
                builder.Append(" ");
            }

            builder.Append(parameter.TypeName);
            builder.Append(" ");

            builder.Append(parameter.ParameterName);

            return builder.ToString();
        }
    }
}
