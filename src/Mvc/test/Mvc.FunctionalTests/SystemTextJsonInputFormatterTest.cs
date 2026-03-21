// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.FunctionalTests;

public class SystemTextJsonInputFormatterTest : JsonInputFormatterTestBase<FormatterWebSite.StartupWithJsonFormatter>
{
    [Fact(Skip = "https://github.com/dotnet/runtime/issues/38539")]
    public override Task JsonInputFormatter_RoundtripsRecordType()
        => base.JsonInputFormatter_RoundtripsRecordType();

    [Fact]
    public override Task JsonInputFormatter_ValidationWithRecordTypes_NoValidationErrors()
        => base.JsonInputFormatter_ValidationWithRecordTypes_NoValidationErrors();

    [Fact]
    public override Task JsonInputFormatter_ValidationWithRecordTypes_ValidationErrors()
        => base.JsonInputFormatter_ValidationWithRecordTypes_ValidationErrors();
}
