// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution.CodeGeneration
{
    public class DefaultTagHelperWriter : TagHelperWriter
    {
        public override void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteExecuteTagHelpers(CSharpRenderingContext context, ExecuteTagHelpersIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteInitializeTagHelperStructure(CSharpRenderingContext context, InitializeTagHelperStructureIRNode node)
        {
            throw new NotImplementedException();
        }

        public override void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node)
        {
            throw new NotImplementedException();
        }
    }
}
