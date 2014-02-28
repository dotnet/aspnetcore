using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ModelStateDictionary : Dictionary<string, ModelState>
    {
        private readonly Dictionary<string, ModelState> _innerDictionary = new Dictionary<string, ModelState>(StringComparer.OrdinalIgnoreCase);

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

        public bool IsValid
        {
            get { return Values.All(modelState => modelState.Errors.Count == 0); }
        }

        public void AddModelError(string key, Exception exception)
        {
            GetModelStateForKey(key).Errors.Add(exception);
        }

        public void AddModelError(string key, string errorMessage)
        {
            GetModelStateForKey(key).Errors.Add(errorMessage);
        }

        public bool IsValidField([NotNull] string key)
        {
            // if the key is not found in the dictionary, we just say that it's valid (since there are no errors)
            foreach (var entry in DictionaryHelper.FindKeysWithPrefix(this, key))
            {
                if (entry.Value.Errors.Count != 0)
                {
                    return false;
                }
            }
            return true;
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
        public void SetModelValue(string key, ValueProviderResult value)
        {
            GetModelStateForKey(key).Value = value;
        }
    }
}