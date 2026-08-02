// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public abstract class GlobalizationTest<TServerFixture> : ServerTestBase<TServerFixture>
    where TServerFixture : ServerFixture
{
    public GlobalizationTest(BrowserFixture browserFixture, TServerFixture serverFixture, ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected abstract void SetCulture(string culture);

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public virtual void CanSetCultureAndParseCultureSensitiveNumbersAndDates(string culture)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        SetCulture(culture);

        // int
        var input = Browser.Exists(By.Id("input_type_text_int"));
        var display = Browser.Exists(By.Id("input_type_text_int_value"));
        Browser.Equal(42.ToString(cultureInfo), () => display.Text);

        input.Clear();
        input.SendKeys(NormalizeWhitespace(9000.ToString("0,000", cultureInfo)));
        input.SendKeys("\t");
        Browser.Equal(9000.ToString(cultureInfo), () => display.Text);

        // decimal
        input = Browser.Exists(By.Id("input_type_text_decimal"));
        display = Browser.Exists(By.Id("input_type_text_decimal_value"));
        Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);

        input.Clear();
        input.SendKeys(NormalizeWhitespace(9000.42m.ToString("0,000.00", cultureInfo)));
        input.SendKeys("\t");
        Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);

        // datetime
        input = Browser.Exists(By.Id("input_type_text_datetime"));
        display = Browser.Exists(By.Id("input_type_text_datetime_value"));
        Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);

        input.ReplaceText(new DateTime(2000, 1, 2).ToString(cultureInfo));
        input.SendKeys("\t");
        Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);

        // datetimeoffset
        input = Browser.Exists(By.Id("input_type_text_datetimeoffset"));
        display = Browser.Exists(By.Id("input_type_text_datetimeoffset_value"));
        Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);

        input.ReplaceText(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
        input.SendKeys("\t");
        Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
    }

    private static string NormalizeWhitespace(string value)
    {
        // In some cultures, the number group separator may be a nonbreaking space. Chrome doesn't let you type a nonbreaking space,
        // so we need to replace it with a normal space.
        return Regex.Replace(value, "\\s", " ");
    }

    // The logic is different for verifying culture-invariant fields. The problem is that the logic for what
    // kinds of text a field accepts is determined by the browser and language - it's not general. So while
    // type="number" and type="date" produce fixed-format and culture-invariant input/output via the "value"
    // attribute - the actual input processing is harder to nail down. In practice this is only a problem
    // with dates.
    //
    // For this reason we avoid sending keys directly to the field, and let two-way binding do its thing instead.
    //
    // A brief summary:
    // 1. Input a value (invariant culture if using number field, or current culture to extra input if using date field)
    // 2. trigger onchange
    // 3. Verify "value" field (current culture)
    // 4. Verify the input field's value attribute (invariant culture)
    //
    // We need to do step 4 to make sure that the value we're entering can "stick" in the form field.
    // We can't use ".Text" because DOM reasons :(
    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void CanSetCultureAndParseCultureInvariantNumbersAndDatesWithInputFields(string culture)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        SetCulture(culture);

        // int
        var input = Browser.Exists(By.Id("input_type_number_int"));
        var display = Browser.Exists(By.Id("input_type_number_int_value"));
        Browser.Equal(42.ToString(cultureInfo), () => display.Text);
        Browser.Equal(42.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(9000.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(9000.ToString(cultureInfo), () => display.Text);
        Browser.Equal(9000.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // decimal
        input = Browser.Exists(By.Id("input_type_number_decimal"));
        display = Browser.Exists(By.Id("input_type_number_decimal_value"));
        Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);
        Browser.Equal(4.2m.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(9000.42m.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);
        Browser.Equal(9000.42m.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // datetime
        input = Browser.Exists(By.Id("input_type_date_datetime"));
        display = Browser.Exists(By.Id("input_type_date_datetime_value"));
        var extraInput = Browser.Exists(By.Id("input_type_date_datetime_extrainput"));
        Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTime(1985, 3, 4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        extraInput.ReplaceText(new DateTime(2000, 1, 2).ToString(cultureInfo));
        extraInput.SendKeys("\t");
        Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTime(2000, 1, 2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // datetimeoffset
        input = Browser.Exists(By.Id("input_type_date_datetimeoffset"));
        display = Browser.Exists(By.Id("input_type_date_datetimeoffset_value"));
        extraInput = Browser.Exists(By.Id("input_type_date_datetimeoffset_extrainput"));
        Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        extraInput.ReplaceText(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
        extraInput.SendKeys("\t");
        Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("fr-FR")]
    public void CanSetCultureAndParseCultureInvariantNumbersAndDatesWithFormComponents(string culture)
    {
        var cultureInfo = CultureInfo.GetCultureInfo(culture);
        SetCulture(culture);

        // int
        var input = Browser.Exists(By.Id("inputnumber_int"));
        var display = Browser.Exists(By.Id("inputnumber_int_value"));
        Browser.Equal(42.ToString(cultureInfo), () => display.Text);
        Browser.Equal(42.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(9000.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(9000.ToString(cultureInfo), () => display.Text);
        Browser.Equal(9000.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // long
        input = Browser.Exists(By.Id("inputnumber_long"));
        display = Browser.Exists(By.Id("inputnumber_long_value"));
        Browser.Equal(4200.ToString(cultureInfo), () => display.Text);
        Browser.Equal(4200.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(90000000000.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(90000000000.ToString(cultureInfo), () => display.Text);
        Browser.Equal(90000000000.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // short
        input = Browser.Exists(By.Id("inputnumber_short"));
        display = Browser.Exists(By.Id("inputnumber_short_value"));
        Browser.Equal(42.ToString(cultureInfo), () => display.Text);
        Browser.Equal(42.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(127.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(127.ToString(cultureInfo), () => display.Text);
        Browser.Equal(127.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // decimal
        input = Browser.Exists(By.Id("inputnumber_decimal"));
        display = Browser.Exists(By.Id("inputnumber_decimal_value"));
        Browser.Equal(4.2m.ToString(cultureInfo), () => display.Text);
        Browser.Equal(4.2m.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        input.Clear();
        input.SendKeys(9000.42m.ToString(CultureInfo.InvariantCulture));
        input.SendKeys("\t");
        Browser.Equal(9000.42m.ToString(cultureInfo), () => display.Text);
        Browser.Equal(9000.42m.ToString(CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // datetime
        input = Browser.Exists(By.Id("inputdate_datetime"));
        display = Browser.Exists(By.Id("inputdate_datetime_value"));
        var extraInput = Browser.Exists(By.Id("inputdate_datetime_extrainput"));
        Browser.Equal(new DateTime(1985, 3, 4).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTime(1985, 3, 4).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        extraInput.ReplaceText(new DateTime(2000, 1, 2).ToString(cultureInfo));
        extraInput.SendKeys("\t");
        Browser.Equal(new DateTime(2000, 1, 2).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTime(2000, 1, 2).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        // datetimeoffset
        input = Browser.Exists(By.Id("inputdate_datetimeoffset"));
        display = Browser.Exists(By.Id("inputdate_datetimeoffset_value"));
        extraInput = Browser.Exists(By.Id("inputdate_datetimeoffset_extrainput"));
        Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTimeOffset(new DateTime(1985, 3, 4)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));

        extraInput.ReplaceText(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo));
        extraInput.SendKeys("\t");
        Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString(cultureInfo), () => display.Text);
        Browser.Equal(new DateTimeOffset(new DateTime(2000, 1, 2)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), () => input.GetDomProperty("value"));
    }
}
