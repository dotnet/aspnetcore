// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc
{
    public class ViewDataDictionary<TModel> : ViewDataDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary{TModel}"/> class.
        /// </summary>
        /// <remarks>
        /// For use when creating a <see cref="ViewDataDictionary{TModel}"/> for a new top-level scope.
        /// </remarks>
        /// <inheritdoc />
        // References may not show up due to ITypeActivator use in RazorPageActivator.
        public ViewDataDictionary(
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] ModelStateDictionary modelState)
            : base(metadataProvider, modelState, declaredModelType: typeof(TModel))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary{TModel}"/> class based in part on an
        /// existing <see cref="ViewDataDictionary"/> instance.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For use when copying a <see cref="ViewDataDictionary"/> instance and <typeparamref name="TModel"/> is known
        /// but <see cref="Model"/> should be copied from the existing instance e.g. when copying from a base
        /// <see cref="ViewDataDictionary"/> instance to a <see cref="ViewDataDictionary{TModel}"/> instance.
        /// </para>
        /// <para>
        /// This constructor may <c>throw</c> if <c>source.Model</c> is non-<c>null</c> and incompatible with
        /// <typeparamref name="TModel"/>. Pass <c>model: null</c> to
        /// <see cref="ViewDataDictionary{TModel}(ViewDataDictionary, object)"/> to ignore <c>source.Model</c>.
        /// </para>
        /// </remarks>
        /// <inheritdoc />
        // References may not show up due to ITypeActivator use in RazorPageActivator.
        public ViewDataDictionary([NotNull] ViewDataDictionary source)
            : base(source, declaredModelType: typeof(TModel))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary{TModel}"/> class based in part on an
        /// existing <see cref="ViewDataDictionary"/> instance. This constructor is careful to avoid exceptions
        /// <see cref="ViewDataDictionary.SetModel"/> may throw when <paramref name="model"/> is <c>null</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For use when copying a <see cref="ViewDataDictionary"/> instance and <typeparamref name="TModel"/> and
        /// <see cref="Model"/> are known.
        /// </para>
        /// <para>
        /// This constructor may <c>throw</c> if <paramref name="model"/> is non-<c>null</c> and incompatible with
        /// <typeparamref name="TModel"/>.
        /// </para>
        /// </remarks>
        /// <inheritdoc />
        // Model parameter type is object to allow "model: null" calls even when TModel is a value type. A TModel
        // parameter would likely require IEquatable<TModel> type restrictions to pass expected null value to the base
        // constructor.
        public ViewDataDictionary([NotNull] ViewDataDictionary source, object model)
            : base(source, model, declaredModelType: typeof(TModel))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary{TModel}"/> class.
        /// </summary>
        /// <remarks>Internal for testing.</remarks>
        /// <inheritdoc />
        internal ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider)
            : base(metadataProvider, declaredModelType: typeof(TModel))
        {
        }

        public new TModel Model
        {
            get
            {
                return (base.Model == null) ? default(TModel) : (TModel)base.Model;
            }
            set
            {
                base.Model = value;
            }
        }
    }
}
