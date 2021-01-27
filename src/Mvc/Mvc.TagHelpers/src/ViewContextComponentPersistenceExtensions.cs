// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// Extensions to <see cref="ViewContext"/> to configure component persistence preferences.
    /// </summary>
    public static class ViewContextComponentPersistenceExtensions
    {
        private static object _persistencePreferenceKey = new();

        /// <summary>
        /// Disables automatic persistence of component applications.
        /// </summary>
        /// <param name="context">The <see cref="ViewContext"/> to set the preference on.</param>
        public static void DisableAutomaticComponentPersistence(this ViewContext context)
        {
            context.Items[_persistencePreferenceKey] = false;
        }

        internal static bool GetAutomaticComponentPersistencePreference(this ViewContext context)
        {
            return context.Items.TryGetValue(_persistencePreferenceKey, out var state) ? (bool)state : true;
        }
    }
}
