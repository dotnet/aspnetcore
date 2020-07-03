// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorProjectEngineFeatureBase : IRazorProjectEngineFeature
    {
        private RazorProjectEngine _projectEngine;

        public virtual RazorProjectEngine ProjectEngine
        {
            get => _projectEngine;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _projectEngine = value;
                OnInitialized();
            }
        }

        protected virtual void OnInitialized()
        {
        }
    }
}
