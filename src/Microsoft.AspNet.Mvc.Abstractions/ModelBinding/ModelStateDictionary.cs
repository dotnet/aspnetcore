// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNet.Mvc.Abstractions;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents the state of an attempt to bind values from an HTTP Request to an action method, which includes
    /// validation information.
    /// </summary>
    public class ModelStateDictionary : IDictionary<string, ModelStateEntry>
    {
        // Make sure to update the doc headers if this value is changed.
        /// <summary>
        /// The default value for <see cref="MaxAllowedErrors"/> of <c>200</c>.
        /// </summary>
        public static readonly int DefaultMaxAllowedErrors = 200;

        private readonly Dictionary<string, ModelStateEntry> _innerDictionary;
        private int _maxAllowedErrors;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class.
        /// </summary>
        public ModelStateDictionary()
            : this(DefaultMaxAllowedErrors)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class.
        /// </summary>
        public ModelStateDictionary(int maxAllowedErrors)
        {
            MaxAllowedErrors = maxAllowedErrors;

            _innerDictionary = new Dictionary<string, ModelStateEntry>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class by using values that are copied
        /// from the specified <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/> to copy values from.</param>
        public ModelStateDictionary(ModelStateDictionary dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            _innerDictionary = new Dictionary<string, ModelStateEntry>(
                dictionary,
                StringComparer.OrdinalIgnoreCase);

            MaxAllowedErrors = dictionary.MaxAllowedErrors;
            ErrorCount = dictionary.ErrorCount;
            HasRecordedMaxModelError = dictionary.HasRecordedMaxModelError;
        }

        /// <summary>
        /// Gets or sets the maximum allowed model state errors in this instance of <see cref="ModelStateDictionary"/>.
        /// Defaults to <c>200</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <see cref="ModelStateDictionary"/> tracks the number of model errors added by calls to
        /// <see cref="AddModelError(string, Exception, ModelMetadata)"/> or
        /// <see cref="TryAddModelError(string, Exception, ModelMetadata)"/>.
        /// Once the value of <code>MaxAllowedErrors - 1</code> is reached, if another attempt is made to add an error,
        /// the error message will be ignored and a <see cref="TooManyModelErrorsException"/> will be added.
        /// </para>
        /// <para>
        /// Errors added via modifying <see cref="ModelStateEntry"/> directly do not count towards this limit.
        /// </para>
        /// </remarks>
        public int MaxAllowedErrors
        {
            get
            {
                return _maxAllowedErrors;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                _maxAllowedErrors = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether or not the maximum number of errors have been
        /// recorded.
        /// </summary>
        /// <remarks>
        /// Returns <c>true</c> if a <see cref="TooManyModelErrorsException"/> has been recorded;
        /// otherwise <c>false</c>.
        /// </remarks>
        public bool HasReachedMaxErrors
        {
            get { return ErrorCount >= MaxAllowedErrors; }
        }

        /// <summary>
        /// Gets the number of errors added to this instance of <see cref="ModelStateDictionary"/> via
        /// <see cref="AddModelError"/> or <see cref="TryAddModelError"/>.
        /// </summary>
        public int ErrorCount { get; private set; }

        /// <inheritdoc />
        public int Count
        {
            get { return _innerDictionary.Count; }
        }

        /// <inheritdoc />
        public bool IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<string, ModelStateEntry>>)_innerDictionary).IsReadOnly; }
        }

        /// <inheritdoc />
        public ICollection<string> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        /// <inheritdoc />
        public ICollection<ModelStateEntry> Values
        {
            get { return _innerDictionary.Values; }
        }

        /// <summary>
        /// Gets a value that indicates whether any model state values in this model state dictionary is invalid or not validated.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return ValidationState == ModelValidationState.Valid || ValidationState == ModelValidationState.Skipped;
            }
        }

        /// <inheritdoc />
        public ModelValidationState ValidationState
        {
            get
            {
                var entries = FindKeysWithPrefix(string.Empty);
                return GetValidity(entries, defaultState: ModelValidationState.Valid);
            }
        }

        /// <inheritdoc />
        public ModelStateEntry this[string key]
        {
            get
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                ModelStateEntry value;
                _innerDictionary.TryGetValue(key, out value);
                return value;
            }
            set
            {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                _innerDictionary[key] = value;
            }
        }

        // For unit testing
        internal IDictionary<string, ModelStateEntry> InnerDictionary
        {
            get { return _innerDictionary; }
        }

        // Flag that indiciates if TooManyModelErrorException has already been added to this dictionary.
        private bool HasRecordedMaxModelError { get; set; }

        /// <summary>
        /// Adds the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/> instance
        /// that is associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
        /// <param name="exception">The <see cref="Exception"/> to add.</param>
        public void AddModelError(string key, Exception exception, ModelMetadata metadata)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            TryAddModelError(key, exception, metadata);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/>
        /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
        /// errors has already been recorded, records a <see cref="TooManyModelErrorsException"/> exception instead.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
        /// <param name="exception">The <see cref="Exception"/> to add.</param>
        /// <returns>
        /// <c>True</c> if the given error was added, <c>false</c> if the error was ignored.
        /// See <see cref="MaxAllowedErrors"/>.
        /// </returns>
        public bool TryAddModelError(string key, Exception exception, ModelMetadata metadata)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (metadata == null)
            {
                throw new ArgumentNullException(nameof(metadata));
            }

            if (ErrorCount >= MaxAllowedErrors - 1)
            {
                EnsureMaxErrorsReachedRecorded();
                return false;
            }

            if (exception is FormatException || exception is OverflowException)
            {
                // Convert FormatExceptions and OverflowExceptions to Invalid value messages.
                ModelStateEntry entry;
                TryGetValue(key, out entry);

                var name = metadata.GetDisplayName();
                string errorMessage;
                if (entry == null)
                {
                    errorMessage = Resources.FormatModelError_InvalidValue_GenericMessage(name);
                }
                else
                {
                    errorMessage = Resources.FormatModelError_InvalidValue_MessageWithModelValue(
                        entry.AttemptedValue,
                        name);
                }

                return TryAddModelError(key, errorMessage);
            }

            ErrorCount++;
            AddModelErrorCore(key, exception);
            return true;
        }

        /// <summary>
        /// Adds the specified <paramref name="errorMessage"/> to the <see cref="ModelStateEntry.Errors"/> instance
        /// that is associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
        /// <param name="errorMessage">The error message to add.</param>
        public void AddModelError(string key, string errorMessage)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            TryAddModelError(key, errorMessage);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="errorMessage"/> to the <see cref="ModelStateEntry.Errors"/>
        /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
        /// errors has already been recorded, records a <see cref="TooManyModelErrorsException"/> exception instead.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
        /// <param name="errorMessage">The error message to add.</param>
        /// <returns>
        /// <c>True</c> if the given error was added, <c>false</c> if the error was ignored.
        /// See <see cref="MaxAllowedErrors"/>.
        /// </returns>
        public bool TryAddModelError(string key, string errorMessage)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (errorMessage == null)
            {
                throw new ArgumentNullException(nameof(errorMessage));
            }

            if (ErrorCount >= MaxAllowedErrors - 1)
            {
                EnsureMaxErrorsReachedRecorded();
                return false;
            }

            ErrorCount++;
            var modelState = GetModelStateForKey(key);
            modelState.ValidationState = ModelValidationState.Invalid;
            modelState.Errors.Add(errorMessage);

            return true;
        }

        /// <summary>
        /// Returns the aggregate <see cref="ModelValidationState"/> for items starting with the
        /// specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to look up model state errors for.</param>
        /// <returns>Returns <see cref="ModelValidationState.Unvalidated"/> if no entries are found for the specified
        /// key, <see cref="ModelValidationState.Invalid"/> if at least one instance is found with one or more model
        /// state errors; <see cref="ModelValidationState.Valid"/> otherwise.</returns>
        public ModelValidationState GetFieldValidationState(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var entries = FindKeysWithPrefix(key);
            return GetValidity(entries, defaultState: ModelValidationState.Unvalidated);
        }

        /// <summary>
        /// Returns <see cref="ModelValidationState"/> for the <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to look up model state errors for.</param>
        /// <returns>Returns <see cref="ModelValidationState.Unvalidated"/> if no entry is found for the specified
        /// key, <see cref="ModelValidationState.Invalid"/> if an instance is found with one or more model
        /// state errors; <see cref="ModelValidationState.Valid"/> otherwise.</returns>
        public ModelValidationState GetValidationState(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ModelStateEntry validationState;
            if (TryGetValue(key, out validationState))
            {
                return validationState.ValidationState;
            }

            return ModelValidationState.Unvalidated;
        }

        /// <summary>
        /// Marks the <see cref="ModelStateEntry.ValidationState"/> for the entry with the specified
        /// <paramref name="key"/> as <see cref="ModelValidationState.Valid"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to mark as valid.</param>
        public void MarkFieldValid(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var modelState = GetModelStateForKey(key);
            if (modelState.ValidationState == ModelValidationState.Invalid)
            {
                throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset);
            }

            modelState.ValidationState = ModelValidationState.Valid;
        }

        /// <summary>
        /// Marks the <see cref="ModelStateEntry.ValidationState"/> for the entry with the specified <paramref name="key"/>
        /// as <see cref="ModelValidationState.Skipped"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelStateEntry"/> to mark as skipped.</param>
        public void MarkFieldSkipped(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var modelState = GetModelStateForKey(key);
            if (modelState.ValidationState == ModelValidationState.Invalid)
            {
                throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset_ToSkipped);
            }

            modelState.ValidationState = ModelValidationState.Skipped;
        }

        /// <summary>
        /// Copies the values from the specified <paramref name="dictionary"/> into this instance, overwriting
        /// existing values if keys are the same.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/> to copy values from.</param>
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

        /// <summary>
        /// Sets the of <see cref="ModelStateEntry.RawValue"/> and <see cref="ModelStateEntry.AttemptedValue"/> for
        /// the <see cref="ModelStateEntry"/> with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key for the <see cref="ModelStateEntry"/> entry.</param>
        /// <param name="rawvalue">The raw value for the <see cref="ModelStateEntry"/> entry.</param>
        /// <param name="attemptedValue">
        /// The values of <param name="rawValue"/> in a comma-separated <see cref="string"/>.
        /// </param>
        public void SetModelValue(string key, object rawValue, string attemptedValue)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            var modelState = GetModelStateForKey(key);
            modelState.RawValue = rawValue;
            modelState.AttemptedValue = attemptedValue;
        }

        /// <summary>
        /// Sets the value for the <see cref="ModelStateEntry"/> with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key for the <see cref="ModelStateEntry"/> entry</param>
        /// <param name="valueProviderResult">
        /// A <see cref="ValueProviderResult"/> with data for the <see cref="ModelStateEntry"/> entry.
        /// </param>
        public void SetModelValue(string key, ValueProviderResult valueProviderResult)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Avoid creating a new array for rawValue if there's only one value.
            object rawValue;
            if (valueProviderResult == ValueProviderResult.None)
            {
                rawValue = null;
            }
            else if (valueProviderResult.Length == 1)
            {
                rawValue = valueProviderResult.Values[0];
            }
            else
            {
                rawValue = valueProviderResult.Values.ToArray();
            }

            SetModelValue(key, rawValue, valueProviderResult.ToString());
        }

        /// <summary>
        /// Clears <see cref="ModelStateDictionary"/> entries that match the key that is passed as parameter.
        /// </summary>
        /// <param name="key">The key of <see cref="ModelStateDictionary"/> to clear.</param>
        public void ClearValidationState(string key)
        {
            // If key is null or empty, clear all entries in the dictionary
            // else just clear the ones that have key as prefix
            var entries = FindKeysWithPrefix(key ?? string.Empty);
            foreach (var entry in entries)
            {
                entry.Value.Errors.Clear();
                entry.Value.ValidationState = ModelValidationState.Unvalidated;
            }
        }

        private ModelStateEntry GetModelStateForKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            ModelStateEntry entry;
            if (!TryGetValue(key, out entry))
            {
                entry = new ModelStateEntry();
                this[key] = entry;
            }

            return entry;
        }

        private static ModelValidationState GetValidity(PrefixEnumerable entries, ModelValidationState defaultState)
        {

            var hasEntries = false;
            var validationState = ModelValidationState.Valid;

            foreach (var entry in entries)
            {
                hasEntries = true;

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

            return hasEntries ? validationState : defaultState;
        }

        private void EnsureMaxErrorsReachedRecorded()
        {
            if (!HasRecordedMaxModelError)
            {
                var exception = new TooManyModelErrorsException(Resources.ModelStateDictionary_MaxModelStateErrors);
                AddModelErrorCore(string.Empty, exception);
                HasRecordedMaxModelError = true;
                ErrorCount++;
            }
        }

        private void AddModelErrorCore(string key, Exception exception)
        {
            var modelState = GetModelStateForKey(key);
            modelState.ValidationState = ModelValidationState.Invalid;
            modelState.Errors.Add(exception);
        }

        /// <inheritdoc />
        public void Add(KeyValuePair<string, ModelStateEntry> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add(string key, ModelStateEntry value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            _innerDictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _innerDictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, ModelStateEntry> item)
        {
            return ((ICollection<KeyValuePair<string, ModelStateEntry>>)_innerDictionary).Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _innerDictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo(KeyValuePair<string, ModelStateEntry>[] array, int arrayIndex)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            ((ICollection<KeyValuePair<string, ModelStateEntry>>)_innerDictionary).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, ModelStateEntry> item)
        {
            return ((ICollection<KeyValuePair<string, ModelStateEntry>>)_innerDictionary).Remove(item);
        }

        /// <inheritdoc />
        public bool Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _innerDictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue(string key, out ModelStateEntry value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return _innerDictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, ModelStateEntry>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static bool StartsWithPrefix(string prefix, string key)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (StringComparer.OrdinalIgnoreCase.Equals(key, prefix))
            {
                return true;
            }

            if (key.Length <= prefix.Length)
            {
                return false;
            }

            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                if (key.StartsWith("[", StringComparison.OrdinalIgnoreCase))
                {
                    var subKey = key.Substring(key.IndexOf('.') + 1);

                    if (!subKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    if (string.Equals(prefix, subKey, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    key = subKey;
                }
                else
                {
                    return false;
                }
            }

            // Everything is prefixed by the empty string
            if (prefix.Length == 0)
            {
                return true;
            }
            else
            {
                var charAfterPrefix = key[prefix.Length];
                switch (charAfterPrefix)
                {
                    case '[':
                    case '.':
                        return true;
                }
            }

            return false;
        }

        public PrefixEnumerable FindKeysWithPrefix(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentNullException(nameof(prefix));
            }

            return new PrefixEnumerable(this, prefix);
        }

        public struct PrefixEnumerable : IEnumerable<KeyValuePair<string, ModelStateEntry>>
        {
            private readonly ModelStateDictionary _dictionary;
            private readonly string _prefix;

            public PrefixEnumerable(ModelStateDictionary dictionary, string prefix)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException(nameof(dictionary));
                }

                if (prefix == null)
                {
                    throw new ArgumentNullException(nameof(prefix));
                }

                _dictionary = dictionary;
                _prefix = prefix;
            }

            public PrefixEnumerator GetEnumerator()
            {
                return _dictionary == null ? new PrefixEnumerator() : new PrefixEnumerator(_dictionary, _prefix);
            }

            IEnumerator<KeyValuePair<string, ModelStateEntry>>
                IEnumerable<KeyValuePair<string, ModelStateEntry>>.GetEnumerator()
            {
                return GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public struct PrefixEnumerator : IEnumerator<KeyValuePair<string, ModelStateEntry>>
        {
            private readonly ModelStateDictionary _dictionary;
            private readonly string _prefix;

            private bool _exactMatchUsed;
            private Dictionary<string, ModelStateEntry>.Enumerator _enumerator;

            public PrefixEnumerator(ModelStateDictionary dictionary, string prefix)
            {
                if (dictionary == null)
                {
                    throw new ArgumentNullException(nameof(dictionary));
                }

                if (prefix == null)
                {
                    throw new ArgumentNullException(nameof(prefix));
                }

                _dictionary = dictionary;
                _prefix = prefix;

                _exactMatchUsed = false;
                _enumerator = default(Dictionary<string, ModelStateEntry>.Enumerator);
                Current = default(KeyValuePair<string, ModelStateEntry>);
            }

            public KeyValuePair<string, ModelStateEntry> Current { get; private set; }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (_dictionary == null)
                {
                    return false;
                }

                // ModelStateDictionary has a behavior where the first 'match' returned from iterating
                // prefixes is the exact match for the prefix (if present). Only after looking for an
                // exact match do we fall back to iteration to find 'starts-with' matches.
                if (!_exactMatchUsed)
                {
                    _exactMatchUsed = true;
                    _enumerator = _dictionary._innerDictionary.GetEnumerator();

                    ModelStateEntry entry;
                    if (_dictionary.TryGetValue(_prefix, out entry))
                    {
                        Current = new KeyValuePair<string, ModelStateEntry>(_prefix, entry);
                        return true;
                    }
                }

                while (_enumerator.MoveNext())
                {
                    if (string.Equals(_prefix, _enumerator.Current.Key, StringComparison.OrdinalIgnoreCase))
                    {
                        // Skip this one. We've already handle the 'exact match' case.
                    }
                    else if (StartsWithPrefix(_prefix, _enumerator.Current.Key))
                    {
                        Current = _enumerator.Current;
                        return true;
                    }
                }

                return false;
            }

            public void Reset()
            {
                _exactMatchUsed = false;
                _enumerator = default(Dictionary<string, ModelStateEntry>.Enumerator);
                Current = default(KeyValuePair<string, ModelStateEntry>);
            }
        }
    }
}