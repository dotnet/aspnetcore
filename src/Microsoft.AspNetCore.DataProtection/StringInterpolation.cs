// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if !DOTNET5_4
// These classes allow using the C# string interpolation feature from .NET 4.5.1.
// They're slimmed-down versions of the classes that exist in .NET 4.6.

using System.Globalization;

namespace System
{
    internal struct FormattableString
    {
        private readonly object[] _arguments;
        public readonly string Format;

        internal FormattableString(string format, params object[] arguments)
        {
            Format = format;
            _arguments = arguments;
        }

        public object[] GetArguments() => _arguments;

        public static string Invariant(FormattableString formattable)
        {
            return String.Format(CultureInfo.InvariantCulture, formattable.Format, formattable.GetArguments());
        }
    }
}

namespace System.Runtime.CompilerServices
{
    internal static class FormattableStringFactory
    {
        public static FormattableString Create(string format, params object[] arguments)
        {
            return new FormattableString(format, arguments);
        }
    }
}

#endif
