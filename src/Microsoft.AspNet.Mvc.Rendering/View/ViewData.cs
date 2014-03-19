using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace Microsoft.AspNet.Mvc.Rendering
{
    public class ViewData : DynamicObject
    {
        private readonly Dictionary<object, dynamic> _data;
        private object _model;
        private ModelMetadata _modelMetadata;
        private IModelMetadataProvider _metadataProvider;

        public ViewData([NotNull] IModelMetadataProvider metadataProvider)
        {
            _data = new Dictionary<object, dynamic>();
            _metadataProvider = metadataProvider;
        }

        public ViewData([NotNull] ViewData source)
        {
            _data = source._data;
            _modelMetadata = source.ModelMetadata;
            _metadataProvider = source.MetadataProvider;
            SetModel(source.Model);
        }

        public object Model
        {
            get { return _model; }
            set { SetModel(value); }
        }

        public dynamic this[string index]
        {
            get
            {
                dynamic result;
                if (_data.TryGetValue(index, out result))
                {
                    result = _data[index];
                }
                else
                {
                    result = null;
                }

                return result;
            }
            set
            {
                _data[index] = value;
            }
        }

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

        /// <summary>
        /// Provider for subclasses that need it to override <see cref="ModelMetadata"/>.
        /// </summary>
        protected IModelMetadataProvider MetadataProvider
        {
            get { return _metadataProvider; }
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            result = _data[binder.Name];

            // We return true here because ViewDataDictionary returns null if the key is not
            // in the dictionary, so we simply pass on the returned value.
            return true;
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            // This cast should always succeed.
            dynamic v = value;
            _data[binder.Name] = v;
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes == null || indexes.Length != 1)
            {
                throw new ArgumentException("Invalid number of indexes");
            }

            object index = indexes[0];
            result = this[(string)index];
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes == null || indexes.Length != 1)
            {
                throw new ArgumentException("Invalid number of indexes");
            }

            object index = indexes[0];

            // This cast should always succeed.
            this[(string)index] = value;
            return true;
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
    }
}
