// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Testing
{
    public class SkippedTestCase : XunitTestCase
    {
        private string _skipReason;

        [Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
        public SkippedTestCase() : base()
        {
        }

        public SkippedTestCase(
            string skipReason,
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay defaultMethodDisplay,
            TestMethodDisplayOptions defaultMethodDisplayOptions,
            ITestMethod testMethod,
            object[] testMethodArguments = null)
            : base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
        {
            _skipReason = skipReason;
        }

        protected override string GetSkipReason(IAttributeInfo factAttribute)
            => _skipReason ?? base.GetSkipReason(factAttribute);

        public override void Deserialize(IXunitSerializationInfo data)
        {
            _skipReason = data.GetValue<string>(nameof(_skipReason));

            // We need to call base after reading our value, because Deserialize will call
            // into GetSkipReason.
            base.Deserialize(data);
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(_skipReason), _skipReason);
        }
    }
}
