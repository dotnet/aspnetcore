// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.ObjectModel;

namespace Microsoft.AspNet.Mvc.Formatters
{
    /// <summary>
    /// Represents a collection of formatters.
    /// </summary>
    /// <typeparam name="TFormatter">The type of formatters in the collection.</typeparam>
    public class FormatterCollection<TFormatter> : Collection<TFormatter>
    {

        /// <summary>
        /// Removes all formatters of the specified type.
        /// </summary>
        /// <typeparam name="T">The type to remove.</typeparam>
        public void RemoveType<T>() where T : TFormatter
        {
            for (var i = Count - 1; i >= 0; i--)
            {
                var formatter = this[i];
                if (formatter is T)
                {
                    RemoveAt(i);
                }
            }
        }
    }
}
