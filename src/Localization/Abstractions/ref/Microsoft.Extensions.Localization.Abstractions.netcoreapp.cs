// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Localization
{
    public partial interface IStringLocalizer
    {
        Microsoft.Extensions.Localization.LocalizedString this[string name] { get; }
        Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] { get; }
        System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures);
    }
    public partial interface IStringLocalizerFactory
    {
        Microsoft.Extensions.Localization.IStringLocalizer Create(string baseName, string location);
        Microsoft.Extensions.Localization.IStringLocalizer Create(System.Type resourceSource);
    }
    public partial interface IStringLocalizer<out T> : Microsoft.Extensions.Localization.IStringLocalizer
    {
    }
    public partial class LocalizedString
    {
        public LocalizedString(string name, string value) { }
        public LocalizedString(string name, string value, bool resourceNotFound) { }
        public LocalizedString(string name, string value, bool resourceNotFound, string searchedLocation) { }
        public string Name { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public bool ResourceNotFound { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string SearchedLocation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public string Value { [System.Runtime.CompilerServices.CompilerGeneratedAttribute] get { throw null; } }
        public static implicit operator string (Microsoft.Extensions.Localization.LocalizedString localizedString) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class StringLocalizerExtensions
    {
        public static System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(this Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer) { throw null; }
        public static Microsoft.Extensions.Localization.LocalizedString GetString(this Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer, string name) { throw null; }
        public static Microsoft.Extensions.Localization.LocalizedString GetString(this Microsoft.Extensions.Localization.IStringLocalizer stringLocalizer, string name, params object[] arguments) { throw null; }
    }
    public partial class StringLocalizer<TResourceSource> : Microsoft.Extensions.Localization.IStringLocalizer, Microsoft.Extensions.Localization.IStringLocalizer<TResourceSource>
    {
        public StringLocalizer(Microsoft.Extensions.Localization.IStringLocalizerFactory factory) { }
        public virtual Microsoft.Extensions.Localization.LocalizedString this[string name] { get { throw null; } }
        public virtual Microsoft.Extensions.Localization.LocalizedString this[string name, params object[] arguments] { get { throw null; } }
        public System.Collections.Generic.IEnumerable<Microsoft.Extensions.Localization.LocalizedString> GetAllStrings(bool includeParentCultures) { throw null; }
    }
}
