// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Text
{
    internal static class TextContentChangedEventArgsExtensions
    {
        public static bool TextChangeOccurred(this TextContentChangedEventArgs args, out (ITextChange firstChange, ITextChange lastChange, string newText, string oldText) changeInformation)
        {
            if (args.Changes.Count > 0)
            {
                var firstChange = args.Changes[0];
                var lastChange = args.Changes[args.Changes.Count - 1];
                var oldLength = lastChange.OldEnd - firstChange.OldPosition;
                var newLength = lastChange.NewEnd - firstChange.NewPosition;
                var newText = args.After.GetText(firstChange.NewPosition, newLength);
                var oldText = args.Before.GetText(firstChange.OldPosition, oldLength);

                var wasChanged = true;
                if (oldLength == newLength)
                {
                    wasChanged = !string.Equals(oldText, newText, StringComparison.Ordinal);
                }

                if (wasChanged)
                {
                    changeInformation = (firstChange, lastChange, newText, oldText);
                    return true;
                }
            }

            changeInformation = default((ITextChange, ITextChange, string, string));
            return false;
        }
    }
}
