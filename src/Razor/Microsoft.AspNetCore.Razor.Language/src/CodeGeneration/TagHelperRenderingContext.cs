// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

internal class TagHelperRenderingContext
{
    private Dictionary<string, string> _renderedBoundAttributes;
    private HashSet<string> _verifiedPropertyDictionaries;

    public Dictionary<string, string> RenderedBoundAttributes
    {
        get
        {
            if (_renderedBoundAttributes == null)
            {
                _renderedBoundAttributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            return _renderedBoundAttributes;
        }
    }

    public HashSet<string> VerifiedPropertyDictionaries
    {
        get
        {
            if (_verifiedPropertyDictionaries == null)
            {
                _verifiedPropertyDictionaries = new HashSet<string>(StringComparer.Ordinal);
            }

            return _verifiedPropertyDictionaries;
        }
    }
}
