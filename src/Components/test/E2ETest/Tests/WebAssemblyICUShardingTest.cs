// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using GlobalizationWasmApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

// Blazor WebAssembly loads ICU (globalization) data for subset of cultures by default.
// This app covers testing this along with verifying the behavior for fallback culture for localized resources.
public class WebAssemblyICUShardingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public WebAssemblyICUShardingTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void LoadingApp_FrenchLanguage_Works()
    {
        // Arrange
        // This verifies the EFIGS icu data set.
        var culture = new CultureInfo("fr-FR");
        Initialize(culture);

        var cultureDisplay = Browser.Exists(By.Id("culture"));
        Assert.Equal(culture.ToString(), cultureDisplay.Text);

        var dateDisplay = Browser.Exists(By.Id("dateTime"));
        Assert.Equal("02/09/2020 00:00:00", dateDisplay.Text);

        var localizedDisplay = Browser.Exists(By.Id("localizedString"));
        Assert.Equal("Bonjour!", localizedDisplay.Text);
    }

    [Theory]
    [InlineData("ko", "ko", "2020. 9. 2. 오전 12:00:00", "안녕하세요")] // ko exists in the CJK data set.
    [InlineData("ko-KR", "ko-KR", "2020. 9. 2. 오전 12:00:00", "안녕하세요")]// ko-KR exists in the CJK data set.
    [InlineData("ko-KO", "ko-KO", "2020. 9. 2. 00:00:00", "안녕하세요")] // ko-KO is custom culture and doesn't exist in the CJK data set.
    [InlineData("ja-JP", "ja-JP", "2020/09/02 0:00:00", "Hello")] // ja-JP exists in the CJK data set, but it doesn't have "Hello" defined in resx file.
    public void LoadingApp_KoreanLanguage_Works(string code, string expectedCurrentCulture, string expectedDate, string expectedText)
    {
        // Arrange
        // This verifies the CJK icu data set.
        var culture = new CultureInfo(code);
        Assert.Equal(culture.ToString(), code);
        Initialize(culture);

        var cultureDisplay = Browser.Exists(By.Id("culture"));
        Assert.Equal(expectedCurrentCulture, cultureDisplay.Text);

        var dateDisplay = Browser.Exists(By.Id("dateTime"));
        Assert.Equal(expectedDate, dateDisplay.Text);

        var localizedDisplay = Browser.Exists(By.Id("localizedString"));
        // The app has a "ko" resx file. This test verifies that we can walk up the culture hierarchy correctly.
        Assert.Equal(expectedText, localizedDisplay.Text);
    }

    [Fact]
    public void LoadingApp_RussianLanguage_Works()
    {
        // Arrange
        // This verifies the non-CJK icu data set.
        var culture = new CultureInfo("ru");
        Initialize(culture);

        var cultureDisplay = Browser.Exists(By.Id("culture"));
        Assert.Equal(culture.ToString(), cultureDisplay.Text);

        var dateDisplay = Browser.Exists(By.Id("dateTime"));
        Assert.Equal("02.09.2020 00:00:00", dateDisplay.Text);

        var localizedDisplay = Browser.Exists(By.Id("localizedString"));
        Assert.Equal("Hello", localizedDisplay.Text); // No localized resources for this culture.
    }

    [Fact]
    public void LoadingApp_KannadaLanguage_Works()
    {
        // Arrange
        // This verifies the non-CJK icu data set.
        var culture = new CultureInfo("kn");
        Initialize(culture);

        var cultureDisplay = Browser.Exists(By.Id("culture"));
        Assert.Equal(culture.ToString(), cultureDisplay.Text);

        var dateDisplay = Browser.Exists(By.Id("dateTime"));
        Assert.Equal("2/9/2020 12:00:00 ಪೂರ್ವಾಹ್ನ", dateDisplay.Text);

        var localizedDisplay = Browser.Exists(By.Id("localizedString"));
        Assert.Equal("ಹಲೋ", localizedDisplay.Text);
    }

    [Fact]
    public void LoadingApp_DynamicallySetLanguageThrows()
    {
        // Arrange
        // This verifies that we complain if the app programtically configures a language.
        Navigate($"{ServerPathBase}/?culture=fr&dotNetCulture=es");

        var errorUi = Browser.Exists(By.Id("blazor-error-ui"));
        Browser.Equal("block", () => errorUi.GetCssValue("display"));

        var expected = "Blazor detected a change in the application's culture that is not supported with the current project configuration.";
        var logs = Browser.GetBrowserLogs(LogLevel.Severe).Select(l => l.Message);
        Assert.True(logs.Any(l => l.Contains(expected)),
            $"Expected to see globalization error message in the browser logs: {string.Join(Environment.NewLine, logs)}.");
    }

    private void Initialize(CultureInfo culture)
    {
        Navigate($"{ServerPathBase}/?culture={culture}");
    }
}
