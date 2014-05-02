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
using System.Threading;

namespace Microsoft.TestCommon
{
    public class CultureReplacer : IDisposable
    {
        private const string _defaultCultureName = "en-GB";
        private const string _defaultUICultureName = "en-US";
        private static readonly CultureInfo _defaultCulture = CultureInfo.GetCultureInfo(_defaultCultureName);
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;
        private readonly long _threadId;

        // Culture => Formatting of dates/times/money/etc, defaults to en-GB because en-US is the same as InvariantCulture
        // We want to be able to find issues where the InvariantCulture is used, but a specific culture should be.
        //
        // UICulture => Language
        public CultureReplacer(string culture = _defaultCultureName, string uiCulture = _defaultUICultureName)
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;
            _threadId = Thread.CurrentThread.ManagedThreadId;

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            Thread.CurrentThread.CurrentUICulture = CultureInfo.GetCultureInfo(uiCulture);
        }

        /// <summary>
        /// The name of the culture that is used as the default value for Thread.CurrentCulture when CultureReplacer is used.
        /// </summary>
        public static string DefaultCultureName
        {
            get { return _defaultCultureName; }
        }

        /// <summary>
        /// The name of the culture that is used as the default value for Thread.UICurrentCulture when CultureReplacer is used.
        /// </summary>
        public static string DefaultUICultureName
        {
            get { return _defaultUICultureName; }
        }

        /// <summary>
        /// The culture that is used as the default value for Thread.CurrentCulture when CultureReplacer is used.
        /// </summary>
        public static CultureInfo DefaultCulture
        {
            get { return _defaultCulture; }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Assert.True(Thread.CurrentThread.ManagedThreadId == _threadId, "The current thread is not the same as the thread invoking the constructor. This should never happen.");
                Thread.CurrentThread.CurrentCulture = _originalCulture;
                Thread.CurrentThread.CurrentUICulture = _originalUICulture;
            }
        }
    }
}
