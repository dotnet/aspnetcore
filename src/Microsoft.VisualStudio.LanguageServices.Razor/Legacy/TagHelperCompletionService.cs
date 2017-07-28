// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Razor
{
    // ----------------------------------------------------------------------------------------------------
    // NOTE: This is only here for VisualStudio binary compatibility. This type should not be used; instead
    // use the Microsoft.CodeAnalysis.Razor variant from Microsoft.CodeAnalysis.Razor.Workspaces
    // ----------------------------------------------------------------------------------------------------
    public abstract class TagHelperCompletionService
    {
        public abstract AttributeCompletionResult GetAttributeCompletions(AttributeCompletionContext completionContext);

        public abstract ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext);
    }
}
