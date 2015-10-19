// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Generic;

namespace Microsoft.AspNet.Localization
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
        public ProviderCultureResult(string culture)
            : this(new List<string> { culture }, new List<string> { culture })
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the respective culture values provided.
        /// </summary>
        /// <param name="culture">The name of the culture to be used for formatting.</param>
        /// <param name="uiCulture"> The name of the ui culture to be used for text, i.e. language.</param>
        public ProviderCultureResult(string culture, string uiCulture)
            : this(new List<string> { culture }, new List<string> { uiCulture })
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object that has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the same culture value.
        /// </summary>
        /// <param name="cultures">The list of cultures to be used for formatting, text, i.e. language.</param>
        public ProviderCultureResult(IList<string> cultures)
            : this(cultures, cultures)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ProviderCultureResult"/> object has its <see cref="Cultures"/> and
        /// <see cref="UICultures"/> properties set to the respective culture values provided.
        /// </summary>
        /// <param name="cultures">The list of cultures to be used for formatting.</param>
        /// <param name="uiCultures">The list of ui cultures to be used for text, i.e. language.</param>
        public ProviderCultureResult(IList<string> cultures, IList<string> uiCultures)
        {
            Cultures = cultures;
            UICultures = uiCultures;
        }

        /// <summary>
        /// Gets the list of cultures to be used for formatting.
        /// </summary>
        public IList<string> Cultures { get; }

        /// <summary>
        /// Gets the list of ui cultures to be used for text, i.e. language;
        /// </summary>
        public IList<string> UICultures { get; }
    }
}
