// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorEngineFeatureBase : IRazorEngineFeature
    {
#pragma warning disable CS0618
        private RazorEngine _engine;
#pragma warning restore CS0618

#pragma warning disable CS0618
        public RazorEngine Engine
#pragma warning restore CS0618
        {
            get { return _engine; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _engine = value;
                OnInitialized();
            }
        }

        protected TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorEngineFeature
        {
            if (Engine == null)
            {
                throw new InvalidOperationException(Resources.FormatFeatureMustBeInitialized(nameof(Engine)));
            }

            var feature = Engine.Features.OfType<TFeature>().FirstOrDefault();
            ThrowForMissingFeatureDependency<TFeature>(feature);

            return feature;
        }

        protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatFeatureDependencyMissing(
                        GetType().Name,
                        typeof(TDocumentDependency).Name,
                        typeof(RazorCodeDocument).Name));
            }
        }

        protected void ThrowForMissingFeatureDependency<TEngineDependency>(TEngineDependency value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatFeatureDependencyMissing(
                        GetType().Name,
                        typeof(TEngineDependency).Name,
#pragma warning disable CS0618
                        typeof(RazorEngine).Name));
#pragma warning restore CS0618
            }
        }

        protected virtual void OnInitialized()
        {
        }
    }
}
