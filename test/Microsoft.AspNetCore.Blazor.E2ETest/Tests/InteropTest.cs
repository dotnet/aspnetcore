using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using BasicTestApp;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class InteropTest : BasicTestAppTestBase
    {
        public InteropTest(
            BrowserFixture browserFixture,
            DevHostServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            Navigate(ServerPathBase, noReload: true);
            MountTestComponent<InteropComponent>();
        }

        [Fact]
        public void CanInvokeDotNetMethods()
        {
            // Arrange
            var expectedValues = new Dictionary<string, string>
            {
                ["VoidParameterless"] = "[]",
                ["VoidWithOneParameter"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
                ["VoidWithTwoParameters"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
                ["VoidWithThreeParameters"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,6]",
                ["VoidWithFourParameters"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,8,16]",
                ["VoidWithFiveParameters"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,10,20,40]",
                ["VoidWithSixParameters"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,12,24,48,6.25]",
                ["VoidWithSevenParameters"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,14,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
                ["VoidWithEightParameters"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,16,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
                ["VoidParameterlessAsync"] = "[]",
                ["VoidWithOneParameterAsync"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
                ["VoidWithTwoParametersAsync"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
                ["VoidWithThreeParametersAsync"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,6]",
                ["VoidWithFourParametersAsync"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,8,16]",
                ["VoidWithFiveParametersAsync"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,10,20,40]",
                ["VoidWithSixParametersAsync"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,12,24,48,6.25]",
                ["VoidWithSevenParametersAsync"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,14,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
                ["VoidWithEightParametersAsync"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,16,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
                ["result1"] = @"[0.1,0.2]",
                ["result2"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
                ["result3"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
                ["result4"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,6]",
                ["result5"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,8,16]",
                ["result6"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,10,20,40]",
                ["result7"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,12,24,48,6.25]",
                ["result8"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,14,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
                ["result9"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,16,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
                ["result1Async"] = @"[0.1,0.2]",
                ["result2Async"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
                ["result3Async"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
                ["result4Async"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,6]",
                ["result5Async"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,8,16]",
                ["result6Async"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,10,20,40]",
                ["result7Async"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,12,24,48,6.25]",
                ["result8Async"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,14,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
                ["result9Async"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,16,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
                ["ThrowException"] = @"""Threw an exception!""",
                ["AsyncThrowSyncException"] = @"""Threw a sync exception!""",
                ["AsyncThrowAsyncException"] = @"""Threw an async exception!""",
                ["ExceptionFromSyncMethod"] = "Function threw an exception!",
                ["SyncExceptionFromAsyncMethod"] = "Function threw a sync exception!",
                ["AsyncExceptionFromAsyncMethod"] = "Function threw an async exception!",
            };
            var actualValues = new Dictionary<string, string>();

            // Act
            var interopButton = Browser.FindElement(By.Id("btn-interop"));
            interopButton.Click();

            var wait = new WebDriverWait(Browser, TimeSpan.FromSeconds(10))
                .Until(d => d.FindElement(By.Id("done-with-interop")));

            foreach (var expectedValue in expectedValues)
            {
                var currentValue = Browser.FindElement(By.Id(expectedValue.Key));
                actualValues.Add(expectedValue.Key, currentValue.Text);
            }

            // Assert
            foreach (var expectedValue in expectedValues)
            {
                if (expectedValue.Key.Contains("Exception"))
                {
                    Assert.StartsWith(expectedValue.Value, actualValues[expectedValue.Key]);
                }
                else
                {
                    Assert.Equal(expectedValue.Value, actualValues[expectedValue.Key]);
                }
            }
        }
    }
}
