// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public class TagHelperRenderingContext
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
}
