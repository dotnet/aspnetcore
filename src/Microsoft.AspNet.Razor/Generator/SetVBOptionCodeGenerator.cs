// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser.SyntaxTree;

namespace Microsoft.AspNet.Razor.Generator
{
    public class SetVBOptionCodeGenerator : SpanCodeGenerator
    {
        public static readonly string StrictCodeDomOptionName = "AllowLateBound";
        public static readonly string ExplicitCodeDomOptionName = "RequireVariableDeclaration";

        public SetVBOptionCodeGenerator(string optionName, bool value)
        {
            OptionName = optionName;
            Value = value;
        }

        // CodeDOM Option Name, which is NOT the same as the VB Option Name
        public string OptionName { get; private set; }
        public bool Value { get; private set; }

        public static SetVBOptionCodeGenerator Strict(bool onOffValue)
        {
            // Strict On = AllowLateBound Off
            return new SetVBOptionCodeGenerator(StrictCodeDomOptionName, !onOffValue);
        }

        public static SetVBOptionCodeGenerator Explicit(bool onOffValue)
        {
            return new SetVBOptionCodeGenerator(ExplicitCodeDomOptionName, onOffValue);
        }

        public override void GenerateCode(Span target, CodeGeneratorContext context)
        {
            context.CompileUnit.UserData[OptionName] = Value;
        }

        public override string ToString()
        {
            return "Option:" + OptionName + "=" + Value;
        }
    }
}
