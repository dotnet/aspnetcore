// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding.Validation;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Sets up default options for <see cref="MvcViewOptions"/>.
    /// </summary>
    public class MvcViewOptionsSetup : ConfigureOptions<MvcViewOptions>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MvcViewOptionsSetup"/>.
        /// </summary>
        public MvcViewOptionsSetup()
            : base(ConfigureMvc)
        {
        }

        public static void ConfigureMvc(MvcViewOptions options)
        {
            // Set up client validators
            options.ClientModelValidatorProviders.Add(new DefaultClientModelValidatorProvider());
            options.ClientModelValidatorProviders.Add(new DataAnnotationsClientModelValidatorProvider());
            options.ClientModelValidatorProviders.Add(new NumericClientModelValidatorProvider());
        }
    }
}
