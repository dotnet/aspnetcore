// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public sealed class ModelValidatedEventArgs : EventArgs
    {
        public ModelValidatedEventArgs([NotNull] ModelValidationContext validationContext,
                                       [NotNull] ModelValidationNode parentNode)
        {
            ValidationContext = validationContext;
            ParentNode = parentNode;
        }

        public ModelValidationContext ValidationContext { get; private set; }

        public ModelValidationNode ParentNode { get; private set; }
    }
}
