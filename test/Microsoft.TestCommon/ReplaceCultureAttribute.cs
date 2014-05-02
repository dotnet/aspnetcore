// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Globalization;
using System.Reflection;
using System.Threading;

namespace Microsoft.TestCommon
{
    /// <summary>
    /// Replaces the current culture and UI culture for the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ReplaceCultureAttribute : Xunit.BeforeAfterTestAttribute
    {
        private CultureInfo _originalCulture;
        private CultureInfo _originalUICulture;

        public ReplaceCultureAttribute()
        {
            Culture = CultureReplacer.DefaultCultureName;
            UICulture = CultureReplacer.DefaultUICultureName;
        }

        /// <summary>
        /// Sets <see cref="Thread.CurrentCulture"/> for the test. Defaults to en-GB.
        /// </summary>
        /// <remarks>
        /// en-GB is used here as the default because en-US is equivalent to the InvariantCulture. We
        /// want to be able to find bugs where we're accidentally relying on the Invariant instead of the
        /// user's culture.
        /// </remarks>
        public string Culture { get; set; }

        /// <summary>
        /// Sets <see cref="Thread.CurrentUICulture"/> for the test. Defaults to en-US.
        /// </summary>
        public string UICulture { get; set; }

        public override void Before(MethodInfo methodUnderTest)
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(Culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(UICulture);
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
