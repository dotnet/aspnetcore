// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Reflection;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    /// <summary>
    /// Replaces the current culture and UI culture for the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaceCultureAttribute : BeforeAfterTestAttribute
    {
        private const string _defaultCultureName = "en-GB";
        private const string _defaultUICultureName = "en-US";
        private CultureInfo _originalCulture;
        private CultureInfo _originalUICulture;

        /// <summary>
        /// Replaces the current culture and UI culture to en-GB and en-US respectively.
        /// </summary>
        public ReplaceCultureAttribute() :
            this(_defaultCultureName, _defaultUICultureName)
        {
        }

        /// <summary>
        /// Replaces the current culture and UI culture based on specified values.
        /// </summary>
        public ReplaceCultureAttribute(string currentCulture, string currentUICulture)
        {
            Culture = new CultureInfo(currentCulture);
            UICulture = new CultureInfo(currentUICulture);
        }

        /// <summary>
        /// The <see cref="CultureInfo.CurrentCulture"/> for the test. Defaults to en-GB.
        /// </summary>
        /// <remarks>
        /// en-GB is used here as the default because en-US is equivalent to the InvariantCulture. We
        /// want to be able to find bugs where we're accidentally relying on the Invariant instead of the
        /// user's culture.
        /// </remarks>
        public CultureInfo Culture { get; }

        /// <summary>
        /// The <see cref="CultureInfo.CurrentUICulture"/> for the test. Defaults to en-US.
        /// </summary>
        public CultureInfo UICulture { get; }

        public override void Before(MethodInfo methodUnderTest)
        {
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;

            CultureInfo.CurrentCulture = Culture;
            CultureInfo.CurrentUICulture = UICulture;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}

