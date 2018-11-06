// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Testing.xunit;

namespace Microsoft.Extensions.Logging.Testing
{
    /// <summary>
    /// WARNING: This attribute should only be used on well understood flaky test failures caused by external issues and should be removed once the underlying issues have been resolved.
    /// This is not intended to be a long term solution to ensure passing of flaky tests but instead a method to improve test reliability without reducing coverage.
    /// Issues should be filed to remove these attributes from affected tests as soon as the underlying issue is fixed.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class RetryTestAttribute : Attribute
    {
        /// <summary>
        /// WARNING: This attribute should only be used on well understood flaky test failures caused by external issues and should be removed once the underlying issues have been resolved.
        /// This is not intended to be a long term solution to ensure passing of flaky tests but instead a method to improve test reliability without reducing coverage.
        /// Issues should be filed to remove these attributes from affected tests as soon as the underlying issue is fixed.
        /// </summary>
        /// <param name="retryPredicateName">The predicate of the format Func&lt;Exception,bool&gt; that is used to determine if the test should be retried</param>
        /// <param name="retryReason">The reason for retrying the test</param>
        /// <param name="retryLimit">The number of retries to attempt before failing the test, for most purposes this this should be kept at 2 to avoid excessive retries.</param>
        public RetryTestAttribute(string retryPredicateName, string retryReason, int retryLimit = 2)
            : this(retryPredicateName, retryReason, OperatingSystems.Linux | OperatingSystems.MacOSX | OperatingSystems.Windows, retryLimit) { }

        /// <summary>
        /// WARNING: This attribute should only be used on well understood flaky test failures caused by external issuesand should be removed once the underlying issues have been resolved.
        /// This is not intended to be a long term solution to ensure passing of flaky tests but instead a method to improve test reliability without reducing coverage.
        /// Issues should be filed to remove these attributes from affected tests as soon as the underlying issue is fixed.
        /// </summary>
        /// <param name="operatingSystems">The os(es) this retry should be attempted on.</param>
        /// <param name="retryPredicateName">The predicate of the format Func&lt;Exception,bool&gt; that is used to determine if the test should be retried</param>
        /// <param name="retryReason">The reason for retrying the test</param>
        /// <param name="retryLimit">The number of retries to attempt before failing the test, for most purposes this this should be kept at 2 to avoid excessive retries.</param>
        public RetryTestAttribute(string retryPredicateName, string retryReason, OperatingSystems operatingSystems, int retryLimit = 2)
        {
            if (string.IsNullOrEmpty(retryPredicateName))
            {
                throw new ArgumentNullException(nameof(RetryPredicateName), "A valid non-empty predicate method name must be provided.");
            }
            if (string.IsNullOrEmpty(retryReason))
            {
                throw new ArgumentNullException(nameof(retryReason), "A valid non-empty reason for retrying the test must be provided.");
            }
            if (retryLimit < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(retryLimit), retryLimit, "Retry count must be positive.");
            }

            OperatingSystems = operatingSystems;
            RetryPredicateName = retryPredicateName;
            RetryReason = retryReason;
            RetryLimit = retryLimit;
        }

        public string RetryPredicateName { get; }

        public string RetryReason { get; }

        public int RetryLimit { get; }

        public OperatingSystems OperatingSystems { get; }
    }
}
