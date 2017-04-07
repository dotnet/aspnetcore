// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Language
{
    internal abstract class RazorEnginePhaseBase : IRazorEnginePhase
    {
        private RazorEngine _engine;

        public RazorEngine Engine
        {
            get { return _engine; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _engine = value;
                OnIntialized();
            }
        }

        public void Execute(RazorCodeDocument codeDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (Engine == null)
            {
                throw new InvalidOperationException(Resources.FormatPhaseMustBeInitialized(nameof(Engine)));
            }

            ExecuteCore(codeDocument);
        }

        protected void ThrowForMissingDependency<T>(T value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(Resources.FormatPhaseDependencyMissing(
                    GetType().Name,
                    typeof(T).Name,
                    typeof(RazorCodeDocument).Name));
            }
        }

        protected virtual void OnIntialized()
        {
        }

        protected abstract void ExecuteCore(RazorCodeDocument codeDocument);
    }
}
