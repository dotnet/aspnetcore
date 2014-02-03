using System;
using System.Globalization;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorCodeParser : CSharpCodeParser
    {
        private const string ModelKeyword = "model";
        private const string GenericTypeFormatString = "{0}<{1}>";
        private SourceLocation? _endInheritsLocation;
        private bool _modelStatementFound;

        public MvcRazorCodeParser()
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

        private void CheckForInheritsAndModelStatements()
        {
            if (_modelStatementFound && _endInheritsLocation.HasValue)
            {
                Context.OnError(_endInheritsLocation.Value, String.Format(CultureInfo.CurrentCulture, "MvcResources.MvcRazorCodeParser_CannotHaveModelAndInheritsKeyword", ModelKeyword));
            }
        }

        protected virtual void ModelDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(ModelKeyword);
            AcceptAndMoveNext();

            SourceLocation endModelLocation = CurrentLocation;

            BaseTypeDirective(
                String.Format(CultureInfo.CurrentCulture,
                              "MvcResources.MvcRazorCodeParser_ModelKeywordMustBeFollowedByTypeName", ModelKeyword),
                CreateModelCodeGenerator);

            if (_modelStatementFound)
            {
                Context.OnError(endModelLocation, String.Format(CultureInfo.CurrentCulture, "MvcResources.MvcRazorCodeParser_OnlyOneModelStatementIsAllowed", ModelKeyword));
            }

            _modelStatementFound = true;

            CheckForInheritsAndModelStatements();
        }

        private SpanCodeGenerator CreateModelCodeGenerator(string model)
        {
            return new SetModelTypeCodeGenerator(model, GenericTypeFormatString);
        }
    }
}
