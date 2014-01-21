// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcCSharpRazorCodeParser : CSharpCodeParser
    {
        private const string ModelKeyword = "model";
        private SourceLocation? _endInheritsLocation;
        private bool _modelStatementFound;

        public MvcCSharpRazorCodeParser()
        {
            MapDirectives(ModelDirective, ModelKeyword);
        }

        protected override void InheritsDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(SyntaxConstants.CSharp.InheritsKeyword);
            AcceptAndMoveNext();
            _endInheritsLocation = CurrentLocation;

            InheritsDirectiveCore();
            CheckForInheritsAndModelStatements();
        }

        protected virtual void ModelDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(ModelKeyword);
            AcceptAndMoveNext();

            SourceLocation endModelLocation = CurrentLocation;

            BaseTypeDirective(
                String.Format(CultureInfo.CurrentCulture,
                    "The '{0}' keyword must be followed by a type name on the same line.", ModelKeyword),
                CreateModelCodeGenerator);

            if (_modelStatementFound)
            {
                Context.OnError(endModelLocation, String.Format(CultureInfo.CurrentCulture, 
                    "Only one '{0}' statement is allowed in a file.", ModelKeyword));
            }

            _modelStatementFound = true;

            CheckForInheritsAndModelStatements();
        }

        private void CheckForInheritsAndModelStatements()
        {
            if (_modelStatementFound && _endInheritsLocation.HasValue)
            {
                Context.OnError(_endInheritsLocation.Value, String.Format(CultureInfo.CurrentCulture,
                    "The 'inherits' keyword is not allowed when a '{0}' keyword is used.", ModelKeyword));
            }
        }

        private SpanCodeGenerator CreateModelCodeGenerator(string model)
        {
            return new SetModelTypeCodeGenerator(model);
        }
    }
}
