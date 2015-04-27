// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelErrorCollection : Collection<ModelError>
    {
        public void Add([NotNull]Exception exception)
        {
            Add(new ModelError(exception));
        }

        public void Add([NotNull]string errorMessage)
        {
            Add(new ModelError(errorMessage));
        }
    }
}
