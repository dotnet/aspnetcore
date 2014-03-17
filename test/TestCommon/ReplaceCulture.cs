using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Xunit.Sdk;

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// Replaces the current culture and UI culture for the test.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class ReplaceCultureAttribute : BeforeAfterTestAttribute
    {
        private const string _defaultCultureName = "en-GB";
        private const string _defaultUICultureName = "en-US";
        private static readonly CultureInfo _defaultCulture = CultureInfo.GetCultureInfo(_defaultCultureName);
        private CultureInfo _originalCulture;
        private CultureInfo _originalUICulture;

        public ReplaceCultureAttribute()
        {
            Culture = _defaultCulture;
            UICulture = _defaultCulture;
        }

        /// <summary>
        /// Sets <see cref="Thread.CurrentCulture"/> for the test. Defaults to en-GB.
        /// </summary>
        /// <remarks>
        /// en-GB is used here as the default because en-US is equivalent to the InvariantCulture. We
        /// want to be able to find bugs where we're accidentally relying on the Invariant instead of the
        /// user's culture.
        /// </remarks>
        public CultureInfo Culture { get; set; }

        /// <summary>
        /// Sets <see cref="Thread.CurrentUICulture"/> for the test. Defaults to en-US.
        /// </summary>
        public CultureInfo UICulture { get; set; }

        public override void Before(MethodInfo methodUnderTest)
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUICulture = Thread.CurrentThread.CurrentUICulture;

            Thread.CurrentThread.CurrentCulture = Culture;
            Thread.CurrentThread.CurrentUICulture = UICulture;
        }

        public override void After(MethodInfo methodUnderTest)
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUICulture;
        }
    }
}
