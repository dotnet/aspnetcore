// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// Represents the state of an attempt to bind values from an HTTP Request to an action method, which includes
    /// validation information.
    /// </summary>
    public class ModelStateDictionary : IDictionary<string, ModelState>
    {
        private readonly IDictionary<string, ModelState> _innerDictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class.
        /// </summary>
        public ModelStateDictionary()
        {
            _innerDictionary = new Dictionary<string, ModelState>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class by using values that are copied
        /// from the specified <paramref name="dictionary"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/> to copy values from.</param>
        public ModelStateDictionary([NotNull] ModelStateDictionary dictionary)
        {
            _innerDictionary = new CopyOnWriteDictionary<string, ModelState>(dictionary,
                                                                             StringComparer.OrdinalIgnoreCase);

            MaxAllowedErrors = dictionary.MaxAllowedErrors;
            ErrorCount = dictionary.ErrorCount;
            HasRecordedMaxModelError = dictionary.HasRecordedMaxModelError;
        }

        /// <summary>
        /// Gets or sets the maximum allowed errors in this instance of <see cref="ModelStateDictionary"/>.
        /// Defaults to <see cref="int.MaxValue"/>.
        /// </summary>
        /// <remarks>
        /// The value of this property is used to track the total number of calls to
        /// <see cref="AddModelError(string, Exception)"/> and <see cref="AddModelError(string, string)"/> after which
        /// an error is thrown for further invocations. Errors added via modifying <see cref="ModelState"/> do not
        /// count towards this limit.
        /// </remarks>
        public int MaxAllowedErrors { get; set; } = int.MaxValue;

        /// <summary>
        /// Gets a flag that determines if the total number of added errors (given by <see cref="ErrorCount"/>) is
        /// fewer than <see cref="MaxAllowedErrors"/>.
        /// </summary>
        public bool CanAddErrors
        {
            get { return ErrorCount < MaxAllowedErrors; }
        }

        /// <summary>
        /// Gets the number of errors added to this instance of <see cref="ModelStateDictionary"/> via
        /// <see cref="AddModelError(string, Exception)"/> and <see cref="AddModelError(string, string)"/>.
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
            get { return _innerDictionary.IsReadOnly; }
        }

        /// <inheritdoc />
        public ICollection<string> Keys
        {
            get { return _innerDictionary.Keys; }
        }

        /// <inheritdoc />
        public ICollection<ModelState> Values
        {
            get { return _innerDictionary.Values; }
        }

        /// <inheritdoc />
        public bool IsValid
        {
            get { return ValidationState == ModelValidationState.Valid; }
        }

        /// <inheritdoc />
        public ModelValidationState ValidationState
        {
            get { return GetValidity(_innerDictionary); }
        }

        /// <inheritdoc />
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
                if (value == null)
                {
                    throw new ArgumentNullException("value");
                }
                _innerDictionary[key] = value;
            }
        }

        // For unit testing
        internal IDictionary<string, ModelState> InnerDictionary
        {
            get { return _innerDictionary; }
        }

        // Flag that indiciates if TooManyModelErrorException has already been added to this dictionary.
        private bool HasRecordedMaxModelError { get; set; }

        /// <summary>
        /// Adds the specified <paramref name="exception"/> to the <see cref="ModelState.Errors"/> instance
        /// that is associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelState"/> to add errors to.</param>
        /// <param name="exception">The <see cref="Exception"/> to add.</param>
        public void AddModelError([NotNull] string key, [NotNull] Exception exception)
        {
            TryAddModelError(key, exception);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="exception"/> to the <see cref="ModelState.Errors"/>
        /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
        /// errors has already been recorded, records a <see cref="TooManyModelErrorsException"/> exception instead.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelState"/> to add errors to.</param>
        /// <param name="exception">The <see cref="Exception"/> to add.</param>
        /// <returns>True if the error was added, false if the dictionary has already recorded 
        /// at least <see cref="MaxAllowedErrors"/> number of errors.</returns>
        /// <remarks>
        /// This method only allows adding up to <see cref="MaxAllowedErrors"/> - 1. <see cref="MaxAllowedErrors"/>nt
        /// invocation would result in adding a <see cref="TooManyModelErrorsException"/> to the dictionary.
        /// </remarks>
        public bool TryAddModelError([NotNull] string key, [NotNull] Exception exception)
        {
            if (ErrorCount >= MaxAllowedErrors - 1)
            {
                EnsureMaxErrorsReachedRecorded();
                return false;
            }

            ErrorCount++;
            AddModelErrorCore(key, exception);
            return true;
        }

        /// <summary>
        /// Adds the specified <paramref name="errorMessage"/> to the <see cref="ModelState.Errors"/> instance
        /// that is associated with the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelState"/> to add errors to.</param>
        /// <param name="errorMessage">The error message to add.</param>
        public void AddModelError([NotNull] string key, [NotNull] string errorMessage)
        {
            TryAddModelError(key, errorMessage);
        }

        /// <summary>
        /// Attempts to add the specified <paramref name="errorMessage"/> to the <see cref="ModelState.Errors"/>
        /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
        /// errors has already been recorded, records a <see cref="TooManyModelErrorsException"/> exception instead.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelState"/> to add errors to.</param>
        /// <param name="errorMessage">The error message to add.</param>
        /// <returns>True if the error was added, false if the dictionary has already recorded 
        /// at least <see cref="MaxAllowedErrors"/> number of errors.</returns>
        /// <remarks>
        /// This method only allows adding up to <see cref="MaxAllowedErrors"/> - 1. <see cref="MaxAllowedErrors"/>nt
        /// invocation would result in adding a <see cref="TooManyModelErrorsException"/> to the dictionary.
        /// </remarks>
        public bool TryAddModelError([NotNull] string key, [NotNull] string errorMessage)
        {
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
        public ModelValidationState GetFieldValidationState([NotNull] string key)
        {
            var entries = DictionaryHelper.FindKeysWithPrefix(this, key);
            if (!entries.Any())
            {
                return ModelValidationState.Unvalidated;
            }

            return GetValidity(entries);
        }

        /// <summary>
        /// Marks the <see cref="ModelState.ValidationState"/> for the entry with the specified <paramref name="key"/>
        /// as <see cref="ModelValidationState.Valid"/>.
        /// </summary>
        /// <param name="key">The key of the <see cref="ModelState"/> to mark as valid.</param>
        public void MarkFieldValid([NotNull] string key)
        {
            var modelState = GetModelStateForKey(key);
            if (modelState.ValidationState == ModelValidationState.Invalid)
            {
                throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset);
            }

            modelState.ValidationState = ModelValidationState.Valid;
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
        /// Sets the value for the <see cref="ModelState"/> with the specified <paramref name="key"/> to the
        /// specified <paramref name="value"/>.
        /// </summary>
        /// <param name="key">The key for the <see cref="ModelState"/> entry.</param>
        /// <param name="value">The value to assign.</param>
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
        public void Add(KeyValuePair<string, ModelState> item)
        {
            Add(item.Key, item.Value);
        }

        /// <inheritdoc />
        public void Add([NotNull] string key, [NotNull] ModelState value)
        {
            _innerDictionary.Add(key, value);
        }

        /// <inheritdoc />
        public void Clear()
        {
            _innerDictionary.Clear();
        }

        /// <inheritdoc />
        public bool Contains(KeyValuePair<string, ModelState> item)
        {
            return _innerDictionary.Contains(item);
        }

        /// <inheritdoc />
        public bool ContainsKey([NotNull] string key)
        {
            return _innerDictionary.ContainsKey(key);
        }

        /// <inheritdoc />
        public void CopyTo([NotNull] KeyValuePair<string, ModelState>[] array, int arrayIndex)
        {
            _innerDictionary.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc />
        public bool Remove(KeyValuePair<string, ModelState> item)
        {
            return _innerDictionary.Remove(item);
        }

        /// <inheritdoc />
        public bool Remove([NotNull] string key)
        {
            return _innerDictionary.Remove(key);
        }

        /// <inheritdoc />
        public bool TryGetValue([NotNull] string key, out ModelState value)
        {
            return _innerDictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<string, ModelState>> GetEnumerator()
        {
            return _innerDictionary.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}