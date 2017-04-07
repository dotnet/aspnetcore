// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language
{
    public abstract class RazorIRPassBase
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

        public virtual int Order { get; }

        protected void ThrowForMissingDocumentDependency<TDocumentDependency>(TDocumentDependency value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatFeatureDependencyMissing(
                        GetType().Name,
                        typeof(TDocumentDependency).Name,
                        typeof(RazorEngine).Name));
            }
        }

        protected void ThrowForMissingEngineDependency<TEngineDependency>(TEngineDependency value)
        {
            if (value == null)
            {
                throw new InvalidOperationException(
                    Resources.FormatFeatureDependencyMissing(
                        GetType().Name,
                        typeof(TEngineDependency).Name,
                        typeof(RazorCodeDocument).Name));
            }
        }

        protected virtual void OnIntialized()
        {
        }

        public void Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (irDocument == null)
            {
                throw new ArgumentNullException(nameof(irDocument));
            }

            if (Engine == null)
            {
                throw new InvalidOperationException(Resources.FormatPhaseMustBeInitialized(nameof(Engine)));
            }

            ExecuteCore(codeDocument, irDocument);
        }

        public abstract void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument);
    }
}
