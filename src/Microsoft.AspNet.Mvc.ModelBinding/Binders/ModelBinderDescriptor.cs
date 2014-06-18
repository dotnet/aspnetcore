// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Encapsulates information that describes an <see cref="IModelBinder"/>.
    /// </summary>
    public class ModelBinderDescriptor
    {
        /// <summary>
        /// Creates a new instance of <see cref="ModelBinderDescriptor"/>.
        /// </summary>
        /// <param name="modelBinderType">A <see cref="IModelBinder/> type that the descriptor represents.</param>
        public ModelBinderDescriptor([NotNull] Type modelBinderType)
        {
            var binderType = typeof(IModelBinder);
            if (!binderType.IsAssignableFrom(modelBinderType))
            {
                var message = Resources.FormatTypeMustDeriveFromType(modelBinderType, binderType.FullName);
                throw new ArgumentException(message, "modelBinderType");
            }

            ModelBinderType = modelBinderType;
        }

        /// <summary>
        /// Creates a new instance of <see cref="ModelBinderDescriptor"/>.
        /// </summary>
        /// <param name="modelBinder">An instance of <see cref="IModelBinder"/> that the descriptor represents.</param>
        public ModelBinderDescriptor([NotNull] IModelBinder modelBinder)
        {
            ModelBinder = modelBinder;
            ModelBinderType = modelBinder.GetType();
        }

        /// <summary>
        /// Gets the type of the <see cref="IModelBinder"/>.
        /// </summary>
        public Type ModelBinderType
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the instance of the <see cref="IModelBinder"/>.
        /// </summary>
        public IModelBinder ModelBinder
        {
            get;
            private set;
        }
    }
}