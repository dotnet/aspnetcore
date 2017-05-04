// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public abstract class RazorPage : RazorPageBase
    {
        public override void EndContext()
        {
        }

        public override void BeginContext(int position, int length, bool isLiteral)
        {
        }

        public override void EnsureRenderedBodyOrSections()
        {
        }
    }
}
