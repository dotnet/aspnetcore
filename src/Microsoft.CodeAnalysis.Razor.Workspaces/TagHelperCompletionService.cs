// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Razor
{
    public abstract class TagHelperCompletionService : ILanguageService
    {
        public abstract AttributeCompletionResult GetAttributeCompletions(AttributeCompletionContext completionContext);

        public abstract ElementCompletionResult GetElementCompletions(ElementCompletionContext completionContext);
    }
}
