using System;
using System.Globalization;
using Microsoft.AspNet.Mvc.Razor.Host;
using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Parser;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class MvcRazorCodeParser : CSharpCodeParser
    {
        private const string GenericTypeFormat = "{0}<{1}>";
        private const string ModelKeyword = "model";
        private readonly string _baseType;
        private SourceLocation? _endInheritsLocation;
        private bool _modelStatementFound;

        public MvcRazorCodeParser(string baseType)
        {
            _baseType = baseType;
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
                Context.OnError(_endInheritsLocation.Value, Resources.MvcRazorCodeParser_CannotHaveModelAndInheritsKeyword(ModelKeyword));
            }
        }

        protected virtual void ModelDirective()
        {
            // Verify we're on the right keyword and accept
            AssertDirective(ModelKeyword);
            AcceptAndMoveNext();

            SourceLocation endModelLocation = CurrentLocation;

            BaseTypeDirective(Resources.MvcRazorCodeParser_ModelKeywordMustBeFollowedByTypeName(ModelKeyword),
                              CreateModelCodeGenerator);

            if (_modelStatementFound)
            {
                Context.OnError(endModelLocation, Resources.MvcRazorCodeParser_OnlyOneModelStatementIsAllowed(ModelKeyword));
            }

            _modelStatementFound = true;

            CheckForInheritsAndModelStatements();
        }

        private SpanCodeGenerator CreateModelCodeGenerator(string model)
        {
            // In the event we have an empty model, the name we generate does not matter since it's a parser error.
            // We'll use the non-generic version of the base type.
            string baseType = String.IsNullOrEmpty(model) ?
                                    _baseType :
                                    String.Format(CultureInfo.InvariantCulture, GenericTypeFormat, _baseType, model);
            return new SetBaseTypeCodeGenerator(baseType);
        }
    }
}
