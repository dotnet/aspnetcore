// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Mvc.ModelBinding;
using Microsoft.AspNet.Mvc.Rendering.Expressions;

namespace Microsoft.AspNet.Mvc
{
    public class ViewDataDictionary : IDictionary<string, object>
    {
        private readonly IDictionary<string, object> _data;
        private object _model;
        private ModelMetadata _modelMetadata;
        private IModelMetadataProvider _metadataProvider;

        public ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider)
            : this(metadataProvider, new ModelStateDictionary())
        {
        }

        public ViewDataDictionary([NotNull] IModelMetadataProvider metadataProvider,
                                  [NotNull] ModelStateDictionary modelState)
            : this(metadataProvider,
                   modelState: modelState,
                   data: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                   templateInfo: new TemplateInfo())
        {
        }

        /// <summary>
        /// <see cref="ViewDataDictionary"/> copy constructor for use when model type does not change or caller will
        /// immediately set the <see cref="Model"/> property.
        /// </summary>
        public ViewDataDictionary([NotNull] ViewDataDictionary source)
            : this(source, source.Model)
        {
        }

        /// <summary>
        /// <see cref="ViewDataDictionary"/> copy constructor for use when model type may change. This avoids
        /// exceptions a derived class may throw when <see cref="SetModel"/> is called.
        /// </summary>
        public ViewDataDictionary([NotNull] ViewDataDictionary source, object model)
            : this(source.MetadataProvider,
                   new ModelStateDictionary(source.ModelState),
                   new CopyOnWriteDictionary<string, object>(source, StringComparer.OrdinalIgnoreCase),
                   new TemplateInfo(source.TemplateInfo))
        {
            // Avoid copying information about the object type. To do so when model==null would confuse the
            // ViewDataDictionary<TModel>.ModelMetadata getter.
            if (source.ModelMetadata?.ModelType != typeof(object))
            {
                _modelMetadata = source.ModelMetadata;
            }

            // If we're constructing a derived ViewDataDictionary<TModel> where TModel is a non-Nullable value type,
            // SetModel will throw if we try to call it with null. We should not throw in that case.
            if (model != null)
            {
                SetModel(model);
            }
        }

        private ViewDataDictionary(IModelMetadataProvider metadataProvider,
                                   ModelStateDictionary modelState,
                                   IDictionary<string, object> data,
                                   TemplateInfo templateInfo)
        {
            _metadataProvider = metadataProvider;
            ModelState = modelState;
            _data = data;
            TemplateInfo = templateInfo;
        }

        public object Model
        {
            get { return _model; }
            set { SetModel(value); }
        }

        public ModelStateDictionary ModelState { get; private set; }

        public virtual ModelMetadata ModelMetadata
        {
            get
            {
                return _modelMetadata;
            }
            set
            {
                _modelMetadata = value;
            }
        }

        public TemplateInfo TemplateInfo { get; private set; }

        /// <summary>
        /// Provider for subclasses that need it to override <see cref="ModelMetadata"/>.
        /// </summary>
        protected IModelMetadataProvider MetadataProvider
        {
            get { return _metadataProvider; }
        }

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

        public object Eval(string expression)
        {
            var info = GetViewDataInfo(expression);
            return (info != null) ? info.Value : null;
        }

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

        public ViewDataInfo GetViewDataInfo(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                throw new ArgumentException(Resources.ArgumentCannotBeNullOrEmpty, "expression");
            }

            return ViewDataEvaluator.Eval(this, expression);
        }

        // This method will execute before the derived type's instance constructor executes. Derived types must
        // be aware of this and should plan accordingly. For example, the logic in SetModel() should be simple
        // enough so as not to depend on the "this" pointer referencing a fully constructed object.
        protected virtual void SetModel(object value)
        {
            _model = value;
            if (value == null)
            {
                // Unable to determine model metadata.
                _modelMetadata = null;
            }
            else if (_modelMetadata == null || value.GetType() != ModelMetadata.ModelType)
            {
                // Reset or override model metadata based on new value type.
                _modelMetadata = _metadataProvider.GetMetadataForType(() => value, value.GetType());
            }
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
