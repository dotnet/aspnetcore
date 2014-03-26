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
            get { return Values.All(modelState => modelState.Errors.Count == 0); }
        }

        public ModelState this[[NotNull] string key]
        {
            get
            {
                ModelState value;
                _innerDictionary.TryGetValue(key, out value);
                return value;
            }
            set { _innerDictionary[key] = value; }
        }

        public void AddModelError([NotNull] string key, [NotNull] Exception exception)
        {
            GetModelStateForKey(key).Errors.Add(exception);
        }

        public void AddModelError([NotNull] string key, [NotNull] string errorMessage)
        {
            GetModelStateForKey(key).Errors.Add(errorMessage);
        }

        public bool IsValidField([NotNull] string key)
        {
            // if the key is not found in the dictionary, we just say that it's valid (since there are no errors)
            foreach (var entry in DictionaryHelper.FindKeysWithPrefix(_innerDictionary, key))
            {
                if (entry.Value.Errors.Count != 0)
                {
                    return false;
                }
            }
            return true;
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

        #region IDictionary members
        public void Add(KeyValuePair<string, ModelState> item)
        {
            _innerDictionary.Add(item);
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