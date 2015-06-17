// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.Extensions;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class ViewDataDictionary : IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _data;
        private readonly Type _declaredModelType;
        private readonly IModelMetadataProvider _metadataProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
        /// </summary>
        /// <param name="metadataProvider">
        /// <see cref = "IModelMetadataProvider" /> instance used to calculate
        /// <see cref="ViewDataDictionary.ModelMetadata"/> values.
        /// </param>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> instance for this scope.</param>
        /// <remarks>For use when creating a <see cref="ViewDataDictionary"/> for a new top-level scope.</remarks>
        public ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider,
                                  [NotNull] ModelStateDictionary modelState)
            : this(metadataProvider, modelState, declaredModelType: typeof(object))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based entirely on an existing
        /// instance.
        /// </summary>
        /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
        /// <remarks>
        /// For use when copying a <see cref="ViewDataDictionary"/> instance and the declared <see cref="Model"/>
        /// <see cref="Type"/> will not change e.g. when copying from a <see cref="ViewDataDictionary{TModel}"/>
        /// instance to a base <see cref="ViewDataDictionary"/> instance.
        /// </remarks>
        public ViewDataDictionary([NotNull] ViewDataDictionary source)
            : this(source, source.Model, source._declaredModelType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based in part on an existing
        /// instance. This constructor is careful to avoid exceptions <see cref="SetModel"/> may throw when
        /// <paramref name="model"/> is <c>null</c>.
        /// </summary>
        /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
        /// <param name="model">Value for the <see cref="Model"/> property.</param>
        /// <remarks>
        /// For use when the new instance's declared <see cref="Model"/> <see cref="Type"/> is unknown but its
        /// <see cref="Model"/> is known. In this case, <see cref="object"/> is the best possible guess about the
        /// declared type when <paramref name="model"/> is <c>null</c>.
        /// </remarks>
        public ViewDataDictionary([NotNull] ViewDataDictionary source, object model)
            : this(source, model, declaredModelType: typeof(object))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
        /// </summary>
        /// <param name="metadataProvider">
        /// <see cref="IModelMetadataProvider"/> instance used to calculate
        /// <see cref="ViewDataDictionary.ModelMetadata"/> values.
        /// </param>
        /// <remarks>Internal for testing.</remarks>
        internal ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider)
            : this(metadataProvider, new ModelStateDictionary())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
        /// </summary>
        /// <param name="metadataProvider">
        /// <see cref = "IModelMetadataProvider" /> instance used to calculate
        /// <see cref="ViewDataDictionary.ModelMetadata"/> values.
        /// </param>
        /// <param name="declaredModelType">
        /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set
        /// <see cref="ViewDataDictionary.ModelMetadata"/> when <see cref="Model"/> is <c>null</c>.
        /// </param>
        /// <remarks>
        /// For use when creating a derived <see cref="ViewDataDictionary"/> for a new top-level scope.
        /// </remarks>
        protected ViewDataDictionary(
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] Type declaredModelType)
            : this(metadataProvider, new ModelStateDictionary(), declaredModelType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class.
        /// </summary>
        /// <param name="metadataProvider">
        /// <see cref = "IModelMetadataProvider" /> instance used to calculate
        /// <see cref="ViewDataDictionary.ModelMetadata"/> values.
        /// </param>
        /// <param name="modelState"><see cref="ModelStateDictionary"/> instance for this scope.</param>
        /// <param name="declaredModelType">
        /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set
        /// <see cref="ViewDataDictionary.ModelMetadata"/> when <see cref="Model"/> is <c>null</c>.
        /// </param>
        /// <remarks>
        /// For use when creating a derived <see cref="ViewDataDictionary"/> for a new top-level scope.
        /// </remarks>
        protected ViewDataDictionary(
            [NotNull] IModelMetadataProvider metadataProvider,
            [NotNull] ModelStateDictionary modelState,
            [NotNull] Type declaredModelType)
            : this(metadataProvider,
                   modelState,
                   declaredModelType,
                   data: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                   templateInfo: new TemplateInfo())
        {
            // This is the core constructor called when Model is unknown. Base ModelMetadata on the declared type.
            ModelExplorer = _metadataProvider.GetModelExplorerForType(declaredModelType, model: null);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based in part on an existing
        /// instance.
        /// </summary>
        /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
        /// <param name="declaredModelType">
        /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set
        /// <see cref="ViewDataDictionary.ModelMetadata"/> when <see cref="Model"/> is <c>null</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// For use when copying a <see cref="ViewDataDictionary"/> instance and new instance's declared
        /// <see cref="Model"/> <see cref="Type"/> is known but <see cref="Model"/> should be copied from the existing
        /// instance e.g. when copying from a base <see cref="ViewDataDictionary"/> instance to a
        /// <see cref="ViewDataDictionary{TModel}"/> instance.
        /// </para>
        /// <para>
        /// This constructor may <c>throw</c> if <c>source.Model</c> is non-<c>null</c> and incompatible with
        /// <paramref name="declaredModelType"/>. Pass <c>model: null</c> to
        /// <see cref="ViewDataDictionary(ViewDataDictionary, object, Type)"/> to ignore <c>source.Model</c>.
        /// </para>
        /// </remarks>
        protected ViewDataDictionary([NotNull] ViewDataDictionary source, Type declaredModelType)
            : this(source, model: source.Model, declaredModelType: declaredModelType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ViewDataDictionary"/> class based in part on an existing
        /// instance. This constructor is careful to avoid exceptions <see cref="SetModel"/> may throw when
        /// <paramref name="model"/> is <c>null</c>.
        /// </summary>
        /// <param name="source"><see cref="ViewDataDictionary"/> instance to copy initial values from.</param>
        /// <param name="model">Value for the <see cref="Model"/> property.</param>
        /// <param name="declaredModelType">
        /// <see cref="Type"/> of <see cref="Model"/> values expected. Used to set
        /// <see cref="ViewDataDictionary.ModelMetadata"/> when <see cref="Model"/> is <c>null</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// For use when copying a <see cref="ViewDataDictionary"/> instance and new instance's declared
        /// <see cref="Model"/> <see cref="Type"/> and <see cref="Model"/> are known.
        /// </para>
        /// <para>
        /// This constructor may <c>throw</c> if <paramref name="model"/> is non-<c>null</c> and incompatible with
        /// <paramref name="declaredModelType"/>.
        /// </para>
        /// </remarks>
        protected ViewDataDictionary([NotNull] ViewDataDictionary source, object model, Type declaredModelType)
            : this(source._metadataProvider,
                   new ModelStateDictionary(source.ModelState),
                   declaredModelType,
                   data: new CopyOnWriteDictionary<string, object>(source, StringComparer.OrdinalIgnoreCase),
                   templateInfo: new TemplateInfo(source.TemplateInfo))
        {
            // This is the core constructor called when Model is known.
            var modelType = GetModelType(model);
            if (modelType == source.ModelMetadata.ModelType && model == source.ModelExplorer.Model)
            {
                // Preserve any customizations made to source.ModelExplorer.ModelMetadata if the Type
                // that will be calculated in SetModel() and source.Model match new instance's values.
                ModelExplorer = source.ModelExplorer;
            }
            else if (model == null)
            {
                // Ensure ModelMetadata is never null though SetModel() isn't called below.
                ModelExplorer = _metadataProvider.GetModelExplorerForType(_declaredModelType, model: null);
            }

            // If we're constructing a ViewDataDictionary<TModel> where TModel is a non-Nullable value type,
            // SetModel() will throw if we try to call it with null. We should not throw in that case.
            if (model != null)
            {
                SetModel(model);
            }
        }

        private ViewDataDictionary(
            IModelMetadataProvider metadataProvider,
            ModelStateDictionary modelState,
            Type declaredModelType,
            IDictionary<string, object> data,
            TemplateInfo templateInfo)
        {
            _metadataProvider = metadataProvider;
            ModelState = modelState;
            _declaredModelType = declaredModelType;
            _data = data;
            TemplateInfo = templateInfo;
        }

        public object Model
        {
            get
            {
                return ModelExplorer.Model;
            }
            set
            {
                // Reset ModelExplorer to ensure Model and ModelMetadata.Model remain equal.
                SetModel(value);
            }
        }

        public ModelStateDictionary ModelState { get; }

        /// <summary>
        /// <see cref="ModelMetadata"/> for the current <see cref="Model"/> value or the declared <see cref="Type"/> if
        /// <see cref="Model"/> is <c>null</c>.
        /// </summary>
        /// <remarks>
        /// Value is never <c>null</c> but may describe the <see cref="object"/> class in some cases. This may for
        /// example occur in controllers if <see cref="Model"/> is <c>null</c>.
        /// </remarks>
        public ModelMetadata ModelMetadata
        {
            get
            {
                return ModelExplorer.Metadata;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="ModelExplorer"/> for the <see cref="Model"/>.
        /// </summary>
        public ModelExplorer ModelExplorer { get; set; }

        public TemplateInfo TemplateInfo { get; }

        #region IDictionary properties
        // Do not just pass through to _data: Indexer should not throw a KeyNotFoundException.
        public object this[string index]
        {
            get
            {
                object result;
                _data.TryGetValue(index, out result);
                return result;
            }
            set
            {
                _data[index] = value;
            }
        }

        public int Count
        {
            get { return _data.Count; }
        }

        public bool IsReadOnly
        {
            get { return _data.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return _data.Keys; }
        }

        public ICollection<object> Values
        {
            get { return _data.Values; }
        }
        #endregion

        // for unit testing
        internal IDictionary<string, object> Data
        {
            get { return _data; }
        }

        /// <summary>
        /// Gets value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>Value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>.</returns>
        /// <remarks>
        /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
        /// <see cref="Model"/>.
        /// </remarks>
        public object Eval(string expression)
        {
            var info = GetViewDataInfo(expression);
            return (info != null) ? info.Value : null;
        }

        /// <summary>
        /// Gets value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>, formatted
        /// using given <paramref name="format"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <param name="format">
        /// The composite format <see cref="string"/> (see http://msdn.microsoft.com/en-us/library/txafckwd.aspx).
        /// </param>
        /// <returns>
        /// Value of named <paramref name="expression"/> in this <see cref="ViewDataDictionary"/>, formatted using
        /// given <paramref name="format"/>.
        /// </returns>
        /// <remarks>
        /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
        /// <see cref="Model"/>.
        /// </remarks>
        public string Eval(string expression, string format)
        {
            var value = Eval(expression);
            return FormatValue(value, format);
        }

        public static string FormatValue(object value, string format)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(format))
            {
                return Convert.ToString(value, CultureInfo.CurrentCulture);
            }
            else
            {
                return string.Format(CultureInfo.CurrentCulture, format, value);
            }
        }

        /// <summary>
        /// Gets <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in this
        /// <see cref="ViewDataDictionary"/>.
        /// </summary>
        /// <param name="expression">Expression name, relative to the current model.</param>
        /// <returns>
        /// <see cref="ViewDataInfo"/> for named <paramref name="expression"/> in this
        /// <see cref="ViewDataDictionary"/>.
        /// </returns>
        /// <remarks>
        /// Looks up <paramref name="expression"/> in the dictionary first. Falls back to evaluating it against
        /// <see cref="Model"/>.
        /// </remarks>
        public ViewDataInfo GetViewDataInfo(string expression)
        {
            return ViewDataEvaluator.Eval(this, expression);
        }

        // This method will execute before the derived type's instance constructor executes. Derived types must
        // be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
        // enough so as not to depend on the "this" pointer referencing a fully constructed object.
        protected virtual void SetModel(object value)
        {
            EnsureCompatible(value);

            // Reset or override ModelMetadata based on runtime value type. Fall back to declared type if value is
            // null. When called from the Model setter, ModelMetadata will (temporarily) be null. When called from
            // a constructor, current ModelMetadata may already be set to preserve customizations made in parent scope.
            var modelType = GetModelType(value);
            if (ModelExplorer?.Metadata.ModelType != modelType)
            {
                ModelExplorer = _metadataProvider.GetModelExplorerForType(modelType, value);
            }
            else if (object.ReferenceEquals(value, Model))
            {
                // The metadata already matches, and the model is literally the same, nothing
                // to do here. This will likely occur when using one of the copy constructors.
            }
            else
            {
                // The metadata matches, but it's a new value.
                ModelExplorer = new ModelExplorer(_metadataProvider, ModelExplorer.Container, ModelMetadata, value);
            }
        }

        // Throw if given value is incompatible with the declared Model Type.
        private void EnsureCompatible(object value)
        {
            // IsCompatibleObject verifies if the value is either an instance of _declaredModelType or (if value is
            // null) that _declaredModelType is a nullable type.
            var castWillSucceed = TypeHelper.IsCompatibleWith(_declaredModelType, value);
            if (!castWillSucceed)
            {
                string message;
                if (value == null)
                {
                    message = Resources.FormatViewData_ModelCannotBeNull(_declaredModelType);
                }
                else
                {
                    message = Resources.FormatViewData_WrongTModelType(value.GetType(), _declaredModelType);
                }

                throw new InvalidOperationException(message);
            }
        }

        private Type GetModelType(object value)
        {
            return (value == null) ? _declaredModelType : value.GetType();
        }

        #region IDictionary methods
        public void Add([NotNull] string key, object value)
        {
            _data.Add(key, value);
        }

        public bool ContainsKey([NotNull] string key)
        {
            return _data.ContainsKey(key);
        }

        public bool Remove([NotNull] string key)
        {
            return _data.Remove(key);
        }

        public bool TryGetValue([NotNull] string key, out object value)
        {
            return _data.TryGetValue(key, out value);
        }

        public void Add([NotNull] KeyValuePair<string, object> item)
        {
            _data.Add(item);
        }

        public void Clear()
        {
            _data.Clear();
        }

        public bool Contains([NotNull] KeyValuePair<string, object> item)
        {
            return _data.Contains(item);
        }

        public void CopyTo([NotNull] KeyValuePair<string, object>[] array, int arrayIndex)
        {
            _data.CopyTo(array, arrayIndex);
        }

        public bool Remove([NotNull] KeyValuePair<string, object> item)
        {
            return _data.Remove(item);
        }

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            return _data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _data.GetEnumerator();
        }
        #endregion
    }
}
