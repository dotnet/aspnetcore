// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures
{
    public readonly struct ViewFactoryContext
    {
        public ViewFactoryContext(
            ActionContext actionContext,
            string executingFilePath,
            string name,
            bool isMainPage)
        {
            ActionContext = actionContext;
            ExecutingFilePath = executingFilePath;
            Name = name;
            IsMainPage = isMainPage;
        }

        public ActionContext ActionContext { get; }

        public string ExecutingFilePath { get; }

        public string Name { get; }

        public bool IsMainPage { get; }
    }
}