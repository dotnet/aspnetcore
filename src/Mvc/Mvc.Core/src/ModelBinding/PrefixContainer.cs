// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// This is a container for prefix values. It normalizes all the values into dotted-form and then stores
/// them in a sorted array. All queries for prefixes are also normalized to dotted-form, and searches
/// for ContainsPrefix are done with a binary search.
/// </summary>
public class PrefixContainer
{
    private readonly ICollection<string> _originalValues;
    private readonly string[] _sortedValues;

    /// <summary>
    /// Initializes a new instance of <see cref="PrefixContainer"/>.
    /// </summary>
    /// <param name="values">The values for the container.</param>
    public PrefixContainer(ICollection<string> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        _originalValues = values;

        if (_originalValues.Count == 0)
        {
            _sortedValues = Array.Empty<string>();
        }
        else
        {
            _sortedValues = new string[_originalValues.Count];
            _originalValues.CopyTo(_sortedValues, 0);
            Array.Sort(_sortedValues, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Checks if a prefix is in the container.
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>True if the prefix is present.</returns>
    public bool ContainsPrefix(string prefix)
    {
        ArgumentNullException.ThrowIfNull(prefix);

        if (_sortedValues.Length == 0)
        {
            return false;
        }

        if (prefix.Length == 0)
        {
            return true; // Empty prefix matches all elements.
        }

        return BinarySearch(prefix) > -1;
    }

    /// <summary>
    /// Gets the keys from a prefix.
    /// </summary>
    /// <remarks>
    /// Given "foo.bar", "foo.hello", "something.other", foo[abc].baz and asking for prefix "foo" will return:
    /// - "bar"/"foo.bar"
    /// - "hello"/"foo.hello"
    /// - "abc"/"foo[abc]"
    /// </remarks>
    /// <param name="prefix">The prefix to enumerate.</param>
    /// <returns>The keys for the prefix.</returns>
    public IDictionary<string, string> GetKeysFromPrefix(string prefix)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _originalValues)
        {
            if (entry != null)
            {
                if (entry.Length == prefix.Length)
                {
                    // No key in this entry
                    continue;
                }

                if (prefix.Length == 0)
                {
                    GetKeyFromEmptyPrefix(entry, result);
                }
                else if (entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    GetKeyFromNonEmptyPrefix(prefix, entry, result);
                }
            }
        }

        return result;
    }

    private static void GetKeyFromEmptyPrefix(string entry, IDictionary<string, string> results)
    {
        string key;
        string fullName;
        var delimiterPosition = entry.AsSpan().IndexOfAny('[', '.');

        if (delimiterPosition == 0 && entry[0] == '[')
        {
            // Handle an entry such as "[key]".
            var bracketPosition = entry.IndexOf(']', 1);
            if (bracketPosition == -1)
            {
                // Malformed for dictionary.
                return;
            }

            key = entry.Substring(1, bracketPosition - 1);
            fullName = entry.Substring(0, bracketPosition + 1);
        }
        else
        {
            // Handle an entry such as "key", "key.property" and "key[index]".
            key = delimiterPosition == -1 ? entry : entry.Substring(0, delimiterPosition);
            fullName = key;
        }

        if (!results.ContainsKey(key))
        {
            results.Add(key, fullName);
        }
    }

    private static void GetKeyFromNonEmptyPrefix(string prefix, string entry, IDictionary<string, string> results)
    {
        string key;
        string fullName;
        var keyPosition = prefix.Length + 1;

        switch (entry[prefix.Length])
        {
            case '.':
                // Handle an entry such as "prefix.key", "prefix.key.property" and "prefix.key[index]".
                var delimiterPosition = entry.AsSpan(keyPosition).IndexOfAny('[', '.');
                if (delimiterPosition < 0)
                {
                    // Neither '.' nor '[' found later in the name. Use rest of the string.
                    key = entry.Substring(keyPosition);
                    fullName = entry;
                }
                else
                {
                    key = entry.Substring(keyPosition, delimiterPosition);
                    fullName = entry.Substring(0, delimiterPosition + keyPosition);
                }
                break;

            case '[':
                // Handle an entry such as "prefix[key]".
                var bracketPosition = entry.IndexOf(']', keyPosition);
                if (bracketPosition == -1)
                {
                    // Malformed for dictionary
                    return;
                }

                key = entry.Substring(keyPosition, bracketPosition - keyPosition);
                fullName = entry.Substring(0, bracketPosition + 1);
                break;

            default:
                // Ignore an entry such as "prefixA".
                return;
        }

        if (!results.ContainsKey(key))
        {
            results.Add(key, fullName);
        }
    }

    // This is tightly coupled to the definition at ModelStateDictionary.StartsWithPrefix
    private int BinarySearch(string prefix)
    {
        var start = 0;
        var end = _sortedValues.Length - 1;

        while (start <= end)
        {
            var pivot = start + ((end - start) / 2);
            var candidate = _sortedValues[pivot];
            var compare = string.Compare(
                prefix,
                0,
                candidate,
                0,
                prefix.Length,
                StringComparison.OrdinalIgnoreCase);
            if (compare == 0)
            {
                Debug.Assert(candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                // Okay, now we have a candidate that starts with the prefix. If the candidate is longer than
                // the prefix, we need to look at the next character and see if it's a delimiter.
                if (candidate.Length == prefix.Length)
                {
                    // Exact match
                    return pivot;
                }

                var c = candidate[prefix.Length];
                if (c == '.' || c == '[')
                {
                    // Match, followed by delimiter
                    return pivot;
                }

                // Okay, so the candidate has some extra text. We need to keep searching.
                //
                // Can often assume the candidate string is greater than the prefix e.g. that works for
                //  prefix: product
                //  candidate: productId
                // most of the time because "product", "product.id", etc. will sort earlier than "productId". But,
                // the assumption isn't correct if "product[0]" is also in _sortedValues because that value will
                // sort later than "productId".
                //
                // Fall back to brute force and cover all the cases.
                return LinearSearch(prefix, start, end);
            }

            if (compare > 0)
            {
                start = pivot + 1;
            }
            else
            {
                end = pivot - 1;
            }
        }

        return ~start;
    }

    private int LinearSearch(string prefix, int start, int end)
    {
        for (; start <= end; start++)
        {
            var candidate = _sortedValues[start];
            var compare = string.Compare(
                prefix,
                0,
                candidate,
                0,
                prefix.Length,
                StringComparison.OrdinalIgnoreCase);
            if (compare == 0)
            {
                Debug.Assert(candidate.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                // Okay, now we have a candidate that starts with the prefix. If the candidate is longer than
                // the prefix, we need to look at the next character and see if it's a delimiter.
                if (candidate.Length == prefix.Length)
                {
                    // Exact match
                    return start;
                }

                var c = candidate[prefix.Length];
                if (c == '.' || c == '[')
                {
                    // Match, followed by delimiter
                    return start;
                }

                // Keep checking until we've passed all StartsWith() matches.
            }

            if (compare < 0)
            {
                // Prefix is less than the candidate. No potential matches left.
                break;
            }
        }

        return ~start;
    }
}
