// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase<FormatterWebSite.StartupWithJsonFormatter>
    {
        public SystemTextJsonInputFormatterTest(MvcTestFixture<FormatterWebSite.StartupWithJsonFormatter> fixture)
            : base(fixture)
        {
        }

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38539")]
        public override Task JsonInputFormatter_RoundtripsRecordType()
            => base.JsonInputFormatter_RoundtripsRecordType();

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38539")]
        public override Task JsonInputFormatter_ValidationWithRecordTypes_NoValidationErrors()
            => base.JsonInputFormatter_ValidationWithRecordTypes_NoValidationErrors();

        [Fact(Skip = "https://github.com/dotnet/runtime/issues/38539")]
        public override Task JsonInputFormatter_ValidationWithRecordTypes_ValidationErrors()
            => base.JsonInputFormatter_ValidationWithRecordTypes_ValidationErrors();
    }
}
