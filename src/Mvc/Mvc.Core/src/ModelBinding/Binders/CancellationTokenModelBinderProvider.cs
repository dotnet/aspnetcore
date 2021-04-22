// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Binders
{
    /// <summary>
    /// An <see cref="IModelBinderProvider"/> for <see cref="CancellationToken"/>.
    /// </summary>
    public class CancellationTokenModelBinderProvider : IModelBinderProvider
    {
        // CancellationTokenModelBinder does not have any state. Re-use the same instance for binding.

        private readonly CancellationTokenModelBinder _modelBinder = new();

        /// <inheritdoc />
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context.Metadata.ModelType == typeof(CancellationToken))
            {
                return _modelBinder;
            }

            return null;
        }
    }
}
