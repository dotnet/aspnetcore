// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public partial class DefaultPageApplicationModelProviderTest
    {
        private class PocoModel
        {
            // Just a plain ol' model, nothing to see here.

            [ModelBinder]
            public int IgnoreMe { get; set; }
        }
    }
}
