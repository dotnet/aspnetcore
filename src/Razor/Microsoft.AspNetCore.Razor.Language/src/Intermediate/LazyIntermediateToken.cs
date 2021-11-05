// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal class LazyIntermediateToken : IntermediateToken
{
    public object FactoryArgument { get; set; }
    public Func<object, string> ContentFactory { get; set; }

    public override string Content
    {
        get
        {
            if (base.Content == null && ContentFactory != null)
            {
                Content = ContentFactory(FactoryArgument);
                ContentFactory = null;
            }

            return base.Content;
        }
    }
}
