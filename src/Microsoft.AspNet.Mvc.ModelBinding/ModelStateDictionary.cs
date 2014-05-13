// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelStateDictionary : IDictionary<string, ModelState>
    {
        private readonly IDictionary<string, ModelState> _innerDictionary = new Dictionary<string, ModelState>(StringComparer.OrdinalIgnoreCase);

        public ModelStateDictionary()
        {
        }

        public ModelStateDictionary([NotNull] ModelStateDictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                _innerDictionary.Add(entry.Key, entry.Value);
            }
        }

        #region IDictionary properties
        public int Count
        {
            get { return _innerDictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return _innerDictionary.IsReadOnly; }
        }

        public ICollection<string> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        public ICollection<ModelState> Values
        {
            get { return _innerDictionary.Values; }
        }
        #endregion

        public bool IsValid
        {
            get { return ValidationState == ModelValidationState.Valid; }
        }

        public ModelValidationState ValidationState
        {
            get {  return GetValidity(_innerDictionary); }
        }

        public ModelState this[[NotNull] string key]
        {
            get
            {
                ModelState value;
                _innerDictionary.TryGetValue(key, out value);
                return value;
            }
            set
            {
                if(value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _innerDictionary[key] = value;
            }
        }

        public void AddModelError([NotNull] string key, [NotNull] Exception exception)
        {
            var modelState = GetModelStateForKey(key);
            modelState.ValidationState = ModelValidationState.Invalid;
            modelState.Errors.Add(exception);
        }

        public void AddModelError([NotNull] string key, [NotNull] string errorMessage)
        {
            var modelState = GetModelStateForKey(key);
            modelState.ValidationState = ModelValidationState.Invalid;
            modelState.Errors.Add(errorMessage);
        }

        public ModelValidationState GetFieldValidationState([NotNull] string key)
        {
            var entries = DictionaryHelper.FindKeysWithPrefix(this, key);
            if (!entries.Any())
            {
                return ModelValidationState.Unvalidated;
            }

            return GetValidity(entries);
        }

        public void MarkFieldValid([NotNull] string key)
        {
            var modelState = GetModelStateForKey(key);
            if (modelState.ValidationState == ModelValidationState.Invalid)
            {
                throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset);
            }

            modelState.ValidationState = ModelValidationState.Valid;
        }

        public void Merge(ModelStateDictionary dictionary)
        {
            if (dictionary == null)
            {
                return;
            }

            foreach (var entry in dictionary)
            {
                this[entry.Key] = entry.Value;
            }
        }

        public void SetModelValue([NotNull] string key, [NotNull] ValueProviderResult value)
        {
            GetModelStateForKey(key).Value = value;
        }

        private ModelState GetModelStateForKey([NotNull] string key)
        {
            ModelState modelState;
            if (!TryGetValue(key, out modelState))
            {
                modelState = new ModelState();
                this[key] = modelState;
            }

            return modelState;
        }

        private static ModelValidationState GetValidity(IEnumerable<KeyValuePair<string, ModelState>> entries)
        {
            var validationState = ModelValidationState.Valid;
            foreach (var entry in entries)
            {
                var entryState = entry.Value.ValidationState;
                if (entryState == ModelValidationState.Unvalidated)
                {
                    // If any entries of a field is unvalidated, we'll treat the tree as unvalidated.
                    return entryState;
                }
                else if (entryState == ModelValidationState.Invalid)
                {
                    validationState = entryState;
                }
            }
            return validationState;
        }

        #region IDictionary members
        public void Add(KeyValuePair<string, ModelState> item)
        {
            Add(item.Key, item.Value);
        }

        public void Add([NotNull] string key, [NotNull] ModelState value)
        {
            _innerDictionary.Add(key, value);
        }

        public void Clear()
        {
            _innerDictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, ModelState> item)
        {
            return _innerDictionary.Contains(item);
        }

        public bool ContainsKey([NotNull] string key)
        {
            return _innerDictionary.ContainsKey(key);
        }

        public void CopyTo([NotNull] KeyValuePair<string, ModelState>[] array, int arrayIndex)
        {
            _innerDictionary.CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, ModelState> item)
        {
            return _innerDictionary.Remove(item);
        }

        public bool Remove([NotNull] string key)
        {
            return _innerDictionary.Remove(key);
        }

        public bool TryGetValue([NotNull] string key, out ModelState value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        public IEnumerator<KeyValuePair<string, ModelState>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}