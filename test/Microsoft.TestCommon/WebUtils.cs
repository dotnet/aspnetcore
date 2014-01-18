// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.IO;
using System.Reflection;
using System.Web.UI;

namespace System.Web.WebPages.TestUtils
{
    public static class WebUtils
    {
        /// <summary>
        /// Creates an instance of HttpRuntime and assigns it (using magic) to the singleton instance of HttpRuntime. 
        /// Ensure that the returned value is disposed at the end of the test.
        /// </summary>
        /// <returns>Returns an IDisposable that restores the original HttpRuntime.</returns>
        public static IDisposable CreateHttpRuntime(string appVPath, string appPath = null)
        {
            var runtime = new HttpRuntime();
            var appDomainAppVPathField = typeof(HttpRuntime).GetField("_appDomainAppVPath", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            appDomainAppVPathField.SetValue(runtime, CreateVirtualPath(appVPath));

            if (appPath != null)
            {
                var appDomainAppPathField = typeof(HttpRuntime).GetField("_appDomainAppPath", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                appDomainAppPathField.SetValue(runtime, Path.GetFullPath(appPath));
            }

            GetTheRuntime().SetValue(null, runtime);
            var appDomainIdField = typeof(HttpRuntime).GetField("_appDomainId", BindingFlags.NonPublic | BindingFlags.Instance);
            appDomainIdField.SetValue(runtime, "test");

            return new DisposableAction(RestoreHttpRuntime);
        }

        internal static FieldInfo GetTheRuntime()
        {
            return typeof(HttpRuntime).GetField("_theRuntime", BindingFlags.NonPublic | BindingFlags.Static);
        }

        internal static void RestoreHttpRuntime()
        {
            GetTheRuntime().SetValue(null, null);
        }

        internal static object CreateVirtualPath(string path)
        {
            var vPath = typeof(Page).Assembly.GetType("System.Web.VirtualPath");
            var method = vPath.GetMethod("CreateNonRelativeTrailingSlash", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            return method.Invoke(null, new object[] { path });
        }

        private class DisposableAction : IDisposable
        {
            private Action _action;
            private bool _hasDisposed;

            public DisposableAction(Action action)
            {
                if (action == null)
                {
                    throw new ArgumentNullException("action");
                }
                _action = action;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                // If we were disposed by the finalizer it's because the user didn't use a "using" block, so don't do anything!
                if (disposing)
                {
                    lock (this)
                    {
                        if (!_hasDisposed)
                        {
                            _hasDisposed = true;
                            _action();
                        }
                    }
                }
            }
        }
    }
}
