// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Localization
{
    /// <summary>
    /// Details about the cultures obtained from <see cref="IRequestCultureProvider"/>.
    /// </summary>
    public class ProviderCultureResult
    {
        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object that has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the same culture value.
        /// </summary>
        /// <param name="culture">The name of the culture to be used for formatting, text, i.e. language.</param>
        public ProviderCultureResult(StringSegment culture)
            : this(new List<StringSegment> { culture }, new List<StringSegment> { culture })
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the respective culture values provided.
        /// </summary>
        /// <param name="culture">The name of the culture to be used for formatting.</param>
        /// <param name="uiCulture"> The name of the ui culture to be used for text, i.e. language.</param>
        public ProviderCultureResult(StringSegment culture, StringSegment uiCulture)
            : this(new List<StringSegment> { culture }, new List<StringSegment> { uiCulture })
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object that has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the same culture value.
        /// </summary>
        /// <param name="cultures">The list of cultures to be used for formatting, text, i.e. language.</param>
        public ProviderCultureResult(IList<StringSegment> cultures)
            : this(cultures, cultures)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the respective culture values provided.
        /// </summary>
        /// <param name="cultures">The list of cultures to be used for formatting.</param>
        /// <param name="uiCultures">The list of ui cultures to be used for text, i.e. language.</param>
        public ProviderCultureResult(IList<StringSegment> cultures, IList<StringSegment> uiCultures)
        {
            Cultures = cultures;
            UICultures = uiCultures;
        }

        /// <summary>
        /// Gets the list of cultures to be used for formatting.
        /// </summary>
        public IList<StringSegment> Cultures { get; }

        /// <summary>
        /// Gets the list of ui cultures to be used for text, i.e. language;
        /// </summary>
        public IList<StringSegment> UICultures { get; }
    }
}
