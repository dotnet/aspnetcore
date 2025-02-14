// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Shared;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// Represents the state of an attempt to bind values from an HTTP Request to an action method, which includes
/// validation information.
/// </summary>
[DebuggerDisplay("Entries = {Count}, IsValid = {IsValid}")]
[DebuggerTypeProxy(typeof(ModelStateDictionaryDebugView))]
public class ModelStateDictionary : IReadOnlyDictionary<string, ModelStateEntry?>
{
    // Make sure to update the doc headers if this value is changed.
    /// <summary>
    /// The default value for <see cref="MaxAllowedErrors"/> of <c>200</c>.
    /// </summary>
    public static readonly int DefaultMaxAllowedErrors = 200;

    // internal for testing
    internal const int DefaultMaxRecursionDepth = 32;

    private const char DelimiterDot = '.';
    private const char DelimiterOpen = '[';

    private readonly ModelStateNode _root;
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
        : this(maxAllowedErrors, maxValidationDepth: DefaultMaxRecursionDepth, maxStateDepth: DefaultMaxRecursionDepth)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class.
    /// </summary>
    private ModelStateDictionary(int maxAllowedErrors, int maxValidationDepth, int maxStateDepth)
    {
        MaxAllowedErrors = maxAllowedErrors;
        MaxValidationDepth = maxValidationDepth;
        MaxStateDepth = maxStateDepth;
        var emptySegment = new StringSegment(buffer: string.Empty);
        _root = new ModelStateNode(subKey: emptySegment)
        {
            Key = string.Empty
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStateDictionary"/> class by using values that are copied
    /// from the specified <paramref name="dictionary"/>.
    /// </summary>
    /// <param name="dictionary">The <see cref="ModelStateDictionary"/> to copy values from.</param>
    public ModelStateDictionary(ModelStateDictionary dictionary)
        : this(dictionary?.MaxAllowedErrors ?? DefaultMaxAllowedErrors,
              dictionary?.MaxValidationDepth ?? DefaultMaxRecursionDepth,
              dictionary?.MaxStateDepth ?? DefaultMaxRecursionDepth)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        Merge(dictionary);
    }

    /// <summary>
    /// Root entry for the <see cref="ModelStateDictionary"/>.
    /// </summary>
    public ModelStateEntry Root => _root;

    /// <summary>
    /// Gets or sets the maximum allowed model state errors in this instance of <see cref="ModelStateDictionary"/>.
    /// Defaults to <c>200</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ModelStateDictionary"/> tracks the number of model errors added by calls to
    /// <see cref="AddModelError(string, Exception, ModelMetadata)"/> or
    /// <see cref="TryAddModelError(string, Exception, ModelMetadata)"/>.
    /// Once the value of <c>MaxAllowedErrors - 1</c> is reached, if another attempt is made to add an error,
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
            ArgumentOutOfRangeException.ThrowIfNegative(value);

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
    public bool HasReachedMaxErrors => ErrorCount >= MaxAllowedErrors;

    /// <summary>
    /// Gets the number of errors added to this instance of <see cref="ModelStateDictionary"/> via
    /// <see cref="M:AddModelError"/> or <see cref="M:TryAddModelError"/>.
    /// </summary>
    public int ErrorCount { get; private set; }

    /// <inheritdoc />
    public int Count { get; private set; }

    /// <summary>
    /// Gets the key sequence.
    /// </summary>
    public KeyEnumerable Keys => new KeyEnumerable(this);

    /// <inheritdoc />
    IEnumerable<string> IReadOnlyDictionary<string, ModelStateEntry?>.Keys => Keys;

    /// <summary>
    /// Gets the value sequence.
    /// </summary>
    public ValueEnumerable Values => new ValueEnumerable(this);

    /// <inheritdoc />
    IEnumerable<ModelStateEntry> IReadOnlyDictionary<string, ModelStateEntry?>.Values => Values;

    /// <summary>
    /// Gets a value that indicates whether any model state values in this model state dictionary is invalid or not validated.
    /// </summary>
    public bool IsValid
    {
        get
        {
            var state = ValidationState;
            return state == ModelValidationState.Valid || state == ModelValidationState.Skipped;
        }
    }

    /// <inheritdoc />
    public ModelValidationState ValidationState => GetValidity(_root, currentDepth: 0) ?? ModelValidationState.Valid;

    /// <inheritdoc />
    public ModelStateEntry? this[string key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key);

            TryGetValue(key, out var entry);
            return entry;
        }
    }

    // Flag that indicates if TooManyModelErrorException has already been added to this dictionary.
    private bool HasRecordedMaxModelError { get; set; }

    internal int? MaxValidationDepth { get; set; }

    internal int? MaxStateDepth { get; set; }

    /// <summary>
    /// Adds the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <remarks>
    /// This method allows adding the <paramref name="exception"/> to the current <see cref="ModelStateDictionary"/>
    /// when <see cref="ModelMetadata"/> is not available or the exact <paramref name="exception"/>
    /// must be maintained for later use (even if it is for example a <see cref="FormatException"/>).
    /// Where <see cref="ModelMetadata"/> is available, use <see cref="AddModelError(string, Exception, ModelMetadata)"/> instead.
    /// </remarks>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
    /// <param name="exception">The <see cref="Exception"/> to add.</param>
    /// <returns>
    /// <c>True</c> if the given error was added, <c>false</c> if the error was ignored.
    /// See <see cref="MaxAllowedErrors"/>.
    /// </returns>
    public bool TryAddModelException(string key, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(exception);

        if ((exception is InputFormatterException || exception is ValueProviderException)
           && !string.IsNullOrEmpty(exception.Message))
        {
            // InputFormatterException, ValueProviderException is a signal that the message is safe to expose to clients
            return TryAddModelError(key, exception.Message);
        }

        if (ErrorCount >= MaxAllowedErrors - 1)
        {
            EnsureMaxErrorsReachedRecorded();
            return false;
        }

        AddModelErrorCore(key, exception);
        return true;
    }

    /// <summary>
    /// Adds the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
    /// <param name="exception">The <see cref="Exception"/> to add. Some exception types will be replaced with
    /// a descriptive error message.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
    public void AddModelError(string key, Exception exception, ModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(metadata);

        TryAddModelError(key, exception, metadata);
    }

    /// <summary>
    /// Attempts to add the specified <paramref name="exception"/> to the <see cref="ModelStateEntry.Errors"/>
    /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
    /// <param name="exception">The <see cref="Exception"/> to add. Some exception types will be replaced with
    /// a descriptive error message.</param>
    /// <param name="metadata">The <see cref="ModelMetadata"/> associated with the model.</param>
    /// <returns>
    /// <c>True</c> if the given error was added, <c>false</c> if the error was ignored.
    /// See <see cref="MaxAllowedErrors"/>.
    /// </returns>
    public bool TryAddModelError(string key, Exception exception, ModelMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(metadata);

        if (ErrorCount >= MaxAllowedErrors - 1)
        {
            EnsureMaxErrorsReachedRecorded();
            return false;
        }

        if (exception is FormatException || exception is OverflowException)
        {
            // Convert FormatExceptions and OverflowExceptions to Invalid value messages.
            TryGetValue(key, out var entry);

            // Not using metadata.GetDisplayName() or a single resource to avoid strange messages like
            // "The value '' is not valid." (when no value was provided, not even an empty string) and
            // "The supplied value is invalid for Int32." (when error is for an element or parameter).
            var messageProvider = metadata.ModelBindingMessageProvider;

            var name = metadata.DisplayName ?? metadata.PropertyName;
            string errorMessage;
            if (entry == null && name == null)
            {
                errorMessage = messageProvider.NonPropertyUnknownValueIsInvalidAccessor();
            }
            else if (entry == null)
            {
                errorMessage = messageProvider.UnknownValueIsInvalidAccessor(name!);
            }
            else if (name == null)
            {
                errorMessage = messageProvider.NonPropertyAttemptedValueIsInvalidAccessor(entry.AttemptedValue!);
            }
            else
            {
                errorMessage = messageProvider.AttemptedValueIsInvalidAccessor(entry.AttemptedValue!, name);
            }

            return TryAddModelError(key, errorMessage);
        }
        else if ((exception is InputFormatterException || exception is ValueProviderException)
            && !string.IsNullOrEmpty(exception.Message))
        {
            // InputFormatterException, ValueProviderException is a signal that the message is safe to expose to clients
            return TryAddModelError(key, exception.Message);
        }

        AddModelErrorCore(key, exception);
        return true;
    }

    /// <summary>
    /// Adds the specified <paramref name="errorMessage"/> to the <see cref="ModelStateEntry.Errors"/> instance
    /// that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
    /// <param name="errorMessage">The error message to add.</param>
    public void AddModelError(string key, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(errorMessage);

        TryAddModelError(key, errorMessage);
    }

    /// <summary>
    /// Attempts to add the specified <paramref name="errorMessage"/> to the <see cref="ModelStateEntry.Errors"/>
    /// instance that is associated with the specified <paramref name="key"/>. If the maximum number of allowed
    /// errors has already been recorded, ensures that a <see cref="TooManyModelErrorsException"/> exception is
    /// recorded instead.
    /// </summary>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to add errors to.</param>
    /// <param name="errorMessage">The error message to add.</param>
    /// <returns>
    /// <c>True</c> if the given error was added, <c>false</c> if the error was ignored.
    /// See <see cref="MaxAllowedErrors"/>.
    /// </returns>
    public bool TryAddModelError(string key, string errorMessage)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(errorMessage);

        if (ErrorCount >= MaxAllowedErrors - 1)
        {
            EnsureMaxErrorsReachedRecorded();
            return false;
        }

        var modelState = GetOrAddNode(key);
        Count += !modelState.IsContainerNode ? 0 : 1;
        modelState.ValidationState = ModelValidationState.Invalid;
        modelState.MarkNonContainerNode();
        modelState.Errors.Add(errorMessage);

        ErrorCount++;
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
        ArgumentNullException.ThrowIfNull(key);

        var item = GetNode(key);
        return GetValidity(item, currentDepth: 0) ?? ModelValidationState.Unvalidated;
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
        ArgumentNullException.ThrowIfNull(key);

        if (TryGetValue(key, out var validationState))
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
        ArgumentNullException.ThrowIfNull(key);

        var modelState = GetOrAddNode(key);
        if (modelState.ValidationState == ModelValidationState.Invalid)
        {
            throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset);
        }

        Count += !modelState.IsContainerNode ? 0 : 1;
        modelState.MarkNonContainerNode();
        modelState.ValidationState = ModelValidationState.Valid;
    }

    /// <summary>
    /// Marks the <see cref="ModelStateEntry.ValidationState"/> for the entry with the specified <paramref name="key"/>
    /// as <see cref="ModelValidationState.Skipped"/>.
    /// </summary>
    /// <param name="key">The key of the <see cref="ModelStateEntry"/> to mark as skipped.</param>
    public void MarkFieldSkipped(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var modelState = GetOrAddNode(key);
        if (modelState.ValidationState == ModelValidationState.Invalid)
        {
            throw new InvalidOperationException(Resources.Validation_InvalidFieldCannotBeReset_ToSkipped);
        }

        Count += !modelState.IsContainerNode ? 0 : 1;
        modelState.MarkNonContainerNode();
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

        foreach (var source in dictionary)
        {
            var target = GetOrAddNode(source.Key);
            Count += !target.IsContainerNode ? 0 : 1;
            ErrorCount += source.Value.Errors.Count - target.Errors.Count;
            target.Copy(source.Value);
            target.MarkNonContainerNode();
        }
    }

    /// <summary>
    /// Sets the of <see cref="ModelStateEntry.RawValue"/> and <see cref="ModelStateEntry.AttemptedValue"/> for
    /// the <see cref="ModelStateEntry"/> with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key for the <see cref="ModelStateEntry"/> entry.</param>
    /// <param name="rawValue">The raw value for the <see cref="ModelStateEntry"/> entry.</param>
    /// <param name="attemptedValue">
    /// The values of <paramref name="rawValue"/> in a comma-separated <see cref="string"/>.
    /// </param>
    public void SetModelValue(string key, object? rawValue, string? attemptedValue)
    {
        ArgumentNullException.ThrowIfNull(key);

        var modelState = GetOrAddNode(key);
        Count += !modelState.IsContainerNode ? 0 : 1;
        modelState.RawValue = rawValue;
        modelState.AttemptedValue = attemptedValue;
        modelState.MarkNonContainerNode();
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
        ArgumentNullException.ThrowIfNull(key);

        // Avoid creating a new array for rawValue if there's only one value.
        object? rawValue;
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

    private ModelStateNode? GetNode(string key)
    {
        var current = _root;
        if (key.Length > 0)
        {
            var match = default(MatchResult);
            do
            {
                var subKey = FindNext(key, ref match);
                current = current.GetNode(subKey);

                // Path not found, exit early
                if (current == null)
                {
                    break;
                }

            } while (match.Type != Delimiter.None);
        }

        return current;
    }

    private ModelStateNode GetOrAddNode(string key)
    {
        Debug.Assert(key != null);
        // For a key of the format, foo.bar[0].baz[qux] we'll create the following nodes:
        // foo
        //  -> bar
        //   -> [0]
        //    -> baz
        //     -> [qux]

        var current = _root;
        if (key.Length > 0)
        {
            var currentDepth = 0;
            var match = default(MatchResult);
            do
            {
                if (MaxStateDepth != null && currentDepth >= MaxStateDepth)
                {
                    throw new InvalidOperationException(Resources.FormatModelStateDictionary_MaxModelStateDepth(MaxStateDepth));
                }

                var subKey = FindNext(key, ref match);
                current = current.GetOrAddNode(subKey);
                currentDepth++;

            } while (match.Type != Delimiter.None);

            if (current.Key == null)
            {
                // New Node - Set key
                current.Key = key;
            }
        }

        return current;
    }

    // Shared function factored out for clarity, force inlining to put back in
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static StringSegment FindNext(string key, ref MatchResult currentMatch)
    {
        var index = currentMatch.Index;
        var matchType = Delimiter.None;

        for (; index < key.Length; index++)
        {
            var ch = key[index];
            if (ch == DelimiterDot)
            {
                matchType = Delimiter.Dot;
                break;
            }
            else if (ch == DelimiterOpen)
            {
                matchType = Delimiter.OpenBracket;
                break;
            }
        }

        var keyStart = currentMatch.Type == Delimiter.OpenBracket
            ? currentMatch.Index - 1
            : currentMatch.Index;

        currentMatch.Type = matchType;
        currentMatch.Index = index + 1;

        return new StringSegment(key, keyStart, index - keyStart);
    }

    private ModelValidationState? GetValidity(ModelStateNode? node, int currentDepth)
    {
        if (node == null ||
            (MaxValidationDepth != null && currentDepth >= MaxValidationDepth))
        {
            return null;
        }

        ModelValidationState? validationState = null;
        if (!node.IsContainerNode)
        {
            validationState = ModelValidationState.Valid;
            if (node.ValidationState == ModelValidationState.Unvalidated)
            {
                // If any entries of a field is unvalidated, we'll treat the tree as unvalidated.
                return ModelValidationState.Unvalidated;
            }

            if (node.ValidationState == ModelValidationState.Invalid)
            {
                validationState = node.ValidationState;
            }
        }

        if (node.ChildNodes != null)
        {
            currentDepth++;

            for (var i = 0; i < node.ChildNodes.Count; i++)
            {
                var entryState = GetValidity(node.ChildNodes[i], currentDepth);

                if (entryState == ModelValidationState.Unvalidated)
                {
                    return entryState;
                }

                if (validationState == null || entryState == ModelValidationState.Invalid)
                {
                    validationState = entryState;
                }
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
        }
    }

    private void AddModelErrorCore(string key, Exception exception)
    {
        var modelState = GetOrAddNode(key);
        Count += !modelState.IsContainerNode ? 0 : 1;
        modelState.ValidationState = ModelValidationState.Invalid;
        modelState.MarkNonContainerNode();
        modelState.Errors.Add(exception);

        ErrorCount++;
    }

    /// <summary>
    /// Removes all keys and values from this instance of <see cref="ModelStateDictionary"/>.
    /// </summary>
    public void Clear()
    {
        Count = 0;
        HasRecordedMaxModelError = false;
        ErrorCount = 0;
        _root.Reset();
        _root.ChildNodes?.Clear();
    }

    /// <inheritdoc />
    public bool ContainsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return !GetNode(key)?.IsContainerNode ?? false;
    }

    /// <summary>
    /// Removes the <see cref="ModelStateEntry"/> with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> if the element is successfully removed; otherwise <c>false</c>. This method also
    /// returns <c>false</c> if key was not found.</returns>
    public bool Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var node = GetNode(key);
        if (node?.IsContainerNode == false)
        {
            Count--;
            ErrorCount -= node.Errors.Count;
            node.Reset();
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, [NotNullWhen(true)] out ModelStateEntry? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        var result = GetNode(key);
        if (result?.IsContainerNode == false)
        {
            value = result;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Returns an enumerator that iterates through this instance of <see cref="ModelStateDictionary"/>.
    /// </summary>
    /// <returns>An <see cref="Enumerator"/>.</returns>
    public Enumerator GetEnumerator() => new Enumerator(this, prefix: string.Empty);

    /// <inheritdoc />
    IEnumerator<KeyValuePair<string, ModelStateEntry?>>
        IEnumerable<KeyValuePair<string, ModelStateEntry?>>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// <para>
    /// This API supports the MVC's infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </para>
    /// </summary>
    public static bool StartsWithPrefix(string prefix, string key)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(key);

        if (prefix.Length == 0)
        {
            // Everything is prefixed by the empty string.
            return true;
        }

        if (prefix.Length > key.Length)
        {
            return false; // Not long enough.
        }

        if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (key.Length == prefix.Length)
        {
            // Exact match
            return true;
        }

        var charAfterPrefix = key[prefix.Length];
        if (charAfterPrefix == '.' || charAfterPrefix == '[')
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a <see cref="PrefixEnumerable"/> that iterates over this instance of <see cref="ModelStateDictionary"/>
    /// using the specified <paramref name="prefix"/>.
    /// </summary>
    /// <param name="prefix">The prefix.</param>
    /// <returns>The <see cref="PrefixEnumerable"/>.</returns>
    public PrefixEnumerable FindKeysWithPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        return new PrefixEnumerable(this, prefix);
    }

    private struct MatchResult
    {
        public Delimiter Type;
        public int Index;
    }

    private enum Delimiter
    {
        None = 0,
        Dot,
        OpenBracket
    }

    [DebuggerDisplay("SubKey = {SubKey}, Key = {Key}, ValidationState = {ValidationState}")]
    private sealed class ModelStateNode : ModelStateEntry
    {
        private bool _isContainerNode = true;

        public ModelStateNode(StringSegment subKey)
        {
            SubKey = subKey;
        }

        public List<ModelStateNode>? ChildNodes { get; set; }

        public override IReadOnlyList<ModelStateEntry>? Children => ChildNodes;

        public string Key { get; set; } = default!;

        public StringSegment SubKey { get; }

        public override bool IsContainerNode => _isContainerNode;

        public void MarkNonContainerNode()
        {
            _isContainerNode = false;
        }

        public void Copy(ModelStateEntry entry)
        {
            RawValue = entry.RawValue;
            AttemptedValue = entry.AttemptedValue;
            Errors.Clear();
            for (var i = 0; i < entry.Errors.Count; i++)
            {
                Errors.Add(entry.Errors[i]);
            }

            ValidationState = entry.ValidationState;
        }

        public void Reset()
        {
            _isContainerNode = true;
            RawValue = null;
            AttemptedValue = null;
            ValidationState = ModelValidationState.Unvalidated;
            Errors.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelStateNode? GetNode(StringSegment subKey)
        {
            ModelStateNode? modelStateNode = null;
            if (subKey.Length == 0)
            {
                modelStateNode = this;
            }
            else if (ChildNodes != null)
            {
                var index = BinarySearch(subKey);
                if (index >= 0)
                {
                    modelStateNode = ChildNodes[index];
                }
            }

            return modelStateNode;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ModelStateNode GetOrAddNode(StringSegment subKey)
        {
            ModelStateNode modelStateNode;
            if (subKey.Length == 0)
            {
                modelStateNode = this;
            }
            else if (ChildNodes == null)
            {
                ChildNodes = new List<ModelStateNode>(1);
                modelStateNode = new ModelStateNode(subKey);
                ChildNodes.Add(modelStateNode);
            }
            else
            {
                var index = BinarySearch(subKey);
                if (index >= 0)
                {
                    modelStateNode = ChildNodes[index];
                }
                else
                {
                    modelStateNode = new ModelStateNode(subKey);
                    ChildNodes.Insert(~index, modelStateNode);
                }
            }

            return modelStateNode;
        }

        public override ModelStateEntry? GetModelStateForProperty(string propertyName)
            => GetNode(new StringSegment(propertyName));

        private int BinarySearch(StringSegment searchKey)
        {
            Debug.Assert(ChildNodes != null);

            var low = 0;
            var high = ChildNodes.Count - 1;
            while (low <= high)
            {
                var mid = low + ((high - low) / 2);
                var midKey = ChildNodes[mid].SubKey;
                var result = midKey.Length - searchKey.Length;
                if (result == 0)
                {
                    result = string.Compare(
                        midKey.Buffer,
                        midKey.Offset,
                        searchKey.Buffer,
                        searchKey.Offset,
                        searchKey.Length,
                        StringComparison.OrdinalIgnoreCase);
                }

                if (result == 0)
                {
                    return mid;
                }
                if (result < 0)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return ~low;
        }
    }

    /// <summary>
    /// Enumerates over <see cref="ModelStateDictionary"/> to provide entries that start with the
    /// specified prefix.
    /// </summary>
    public readonly struct PrefixEnumerable : IEnumerable<KeyValuePair<string, ModelStateEntry>>
    {
        private readonly ModelStateDictionary _dictionary;
        private readonly string _prefix;

        /// <summary>
        /// Initializes a new instance of <see cref="PrefixEnumerable"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        /// <param name="prefix">The prefix.</param>
        public PrefixEnumerable(ModelStateDictionary dictionary, string prefix)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(prefix);

            _dictionary = dictionary;
            _prefix = prefix;
        }

        /// <inheritdoc />
        public Enumerator GetEnumerator() => new Enumerator(_dictionary, _prefix);

        IEnumerator<KeyValuePair<string, ModelStateEntry>>
            IEnumerable<KeyValuePair<string, ModelStateEntry>>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// An <see cref="IEnumerator{T}"/> for <see cref="PrefixEnumerable"/>.
    /// </summary>
    public struct Enumerator : IEnumerator<KeyValuePair<string, ModelStateEntry>>
    {
        private readonly ModelStateNode? _rootNode;
        private ModelStateNode _modelStateNode;
        private List<ModelStateNode>? _nodes;
        private int _index;
        private bool _visitedRoot;

        /// <summary>
        /// Intializes a new instance of <see cref="Enumerator"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        /// <param name="prefix">The prefix.</param>
        public Enumerator(ModelStateDictionary dictionary, string prefix)
        {
            ArgumentNullException.ThrowIfNull(dictionary);
            ArgumentNullException.ThrowIfNull(prefix);

            _index = -1;
            _rootNode = dictionary.GetNode(prefix);
            _modelStateNode = default!;
            _nodes = null;
            _visitedRoot = false;
        }

        /// <inheritdoc />
        public KeyValuePair<string, ModelStateEntry> Current =>
            new KeyValuePair<string, ModelStateEntry>(_modelStateNode.Key, _modelStateNode);

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose()
        {
        }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (_rootNode == null)
            {
                return false;
            }

            if (!_visitedRoot)
            {
                // Visit the root node
                _visitedRoot = true;
                if (_rootNode.ChildNodes?.Count > 0)
                {
                    _nodes = new List<ModelStateNode> { _rootNode };
                }

                if (!_rootNode.IsContainerNode)
                {
                    _modelStateNode = _rootNode;
                    return true;
                }
            }

            if (_nodes == null)
            {
                return false;
            }

            while (_nodes.Count > 0)
            {
                var node = _nodes[0];
                if (_index == node.ChildNodes!.Count - 1)
                {
                    // We've exhausted the current sublist.
                    _nodes.RemoveAt(0);
                    _index = -1;
                    continue;
                }
                else
                {
                    _index++;
                }

                var currentChild = node.ChildNodes[_index];
                if (currentChild.ChildNodes?.Count > 0)
                {
                    _nodes.Add(currentChild);
                }

                if (!currentChild.IsContainerNode)
                {
                    _modelStateNode = currentChild;
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _index = -1;
            _nodes?.Clear();
            _visitedRoot = false;
            _modelStateNode = default!;
        }
    }

    /// <summary>
    /// A <see cref="IEnumerable{T}"/> for keys in <see cref="ModelStateDictionary"/>.
    /// </summary>
    public readonly struct KeyEnumerable : IEnumerable<string>
    {
        private readonly ModelStateDictionary _dictionary;

        /// <summary>
        /// Initializes a new instance of <see cref="KeyEnumerable"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        public KeyEnumerable(ModelStateDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public KeyEnumerator GetEnumerator() => new KeyEnumerator(_dictionary, prefix: string.Empty);

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// An <see cref="IEnumerator{T}"/> for keys in <see cref="ModelStateDictionary"/>.
    /// </summary>
    public struct KeyEnumerator : IEnumerator<string>
    {
        private Enumerator _prefixEnumerator;

        /// <summary>
        /// Initializes a new instance of <see cref="KeyEnumerable"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        /// <param name="prefix">The prefix.</param>
        public KeyEnumerator(ModelStateDictionary dictionary, string prefix)
        {
            _prefixEnumerator = new Enumerator(dictionary, prefix);
            Current = default!;
        }

        /// <inheritdoc />
        public string Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _prefixEnumerator.Dispose();

        /// <inheritdoc />
        public bool MoveNext()
        {
            var result = _prefixEnumerator.MoveNext();
            if (result)
            {
                var current = _prefixEnumerator.Current;
                Current = current.Key;
            }
            else
            {
                Current = default!;
            }

            return result;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _prefixEnumerator.Reset();
            Current = default!;
        }
    }

    /// <summary>
    /// An <see cref="IEnumerable"/> for <see cref="ModelStateEntry"/>.
    /// </summary>
    public readonly struct ValueEnumerable : IEnumerable<ModelStateEntry>
    {
        private readonly ModelStateDictionary _dictionary;

        /// <summary>
        /// Initializes a new instance of <see cref="ValueEnumerable"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        public ValueEnumerable(ModelStateDictionary dictionary)
        {
            _dictionary = dictionary;
        }

        /// <inheritdoc />
        public ValueEnumerator GetEnumerator() => new ValueEnumerator(_dictionary, prefix: string.Empty);

        IEnumerator<ModelStateEntry> IEnumerable<ModelStateEntry>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    /// <summary>
    /// An enumerator for <see cref="ModelStateEntry"/>.
    /// </summary>
    public struct ValueEnumerator : IEnumerator<ModelStateEntry>
    {
        private Enumerator _prefixEnumerator;

        /// <summary>
        /// Initializes a new instance of <see cref="ValueEnumerator"/>.
        /// </summary>
        /// <param name="dictionary">The <see cref="ModelStateDictionary"/>.</param>
        /// <param name="prefix">The prefix to enumerate.</param>
        public ValueEnumerator(ModelStateDictionary dictionary, string prefix)
        {
            _prefixEnumerator = new Enumerator(dictionary, prefix);
            Current = default!;
        }

        /// <inheritdoc />
        public ModelStateEntry Current { get; private set; }

        object IEnumerator.Current => Current;

        /// <inheritdoc />
        public void Dispose() => _prefixEnumerator.Dispose();

        /// <inheritdoc />
        public bool MoveNext()
        {
            var result = _prefixEnumerator.MoveNext();
            if (result)
            {
                var current = _prefixEnumerator.Current;
                Current = current.Value;
            }
            else
            {
                Current = default!;
            }

            return result;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _prefixEnumerator.Reset();
            Current = default!;
        }
    }

    private sealed class ModelStateDictionaryDebugView(ModelStateDictionary dictionary)
    {
        private readonly ModelStateDictionary _dictionary = dictionary;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public DictionaryItemDebugView<string, ModelStateEntry?>[] Items => _dictionary.Select(pair => new DictionaryItemDebugView<string, ModelStateEntry?>(pair)).ToArray();
    }
}
