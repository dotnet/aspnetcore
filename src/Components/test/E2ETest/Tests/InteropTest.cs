// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class InteropTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
{
    public InteropTest(
        BrowserFixture browserFixture,
        ToggleExecutionModeServerFixture<Program> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate(ServerPathBase);
        Browser.MountTestComponent<InteropComponent>();
    }

    [Fact]
    public void CanInvokeDotNetMethods()
    {
        // Arrange
        var expectedAsyncValues = new Dictionary<string, string>
        {
            ["VoidParameterlessAsync"] = "[]",
            ["VoidWithOneParameterAsync"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
            ["VoidWithTwoParametersAsync"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
            ["VoidWithThreeParametersAsync"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,123]",
            ["VoidWithFourParametersAsync"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,123,16]",
            ["VoidWithFiveParametersAsync"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,123,20,40]",
            ["VoidWithSixParametersAsync"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,123,24,48,6.25]",
            ["VoidWithSevenParametersAsync"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,123,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
            ["VoidWithEightParametersAsync"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,123,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
            ["result1Async"] = @"[0.1,0.2]",
            ["result2Async"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
            ["result3Async"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
            ["result4Async"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,123]",
            ["result5Async"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,123,16]",
            ["result6Async"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,123,20,40]",
            ["result7Async"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,123,24,48,6.25]",
            ["result8Async"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,123,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
            ["result9Async"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,123,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
            ["roundTripJSObjectReferenceAsync"] = @"""successful""",
            ["invokeDisposedJSObjectReferenceExceptionAsync"] = @"""JS object instance with ID",
            ["roundTripByteArrayAsyncFromJS"] = @"{""0"":1,""1"":5,""2"":7,""3"":17,""4"":200,""5"":138}",
            ["roundTripByteArrayWrapperObjectAsyncFromJS"] = @"{""strVal"":""Some string"",""byteArrayVal"":{""0"":1,""1"":5,""2"":7,""3"":17,""4"":200,""5"":138},""intVal"":42}",
            ["roundTripByteArrayAsyncFromDotNet"] = @"1,5,7,15,35,200",
            ["roundTripByteArrayWrapperObjectAsyncFromDotNet"] = @"StrVal: Some String, IntVal: 100000, ByteArrayVal: 1,5,7,15,35,200",
            ["jsToDotNetStreamReturnValueAsync"] = "Success",
            ["jsToDotNetStreamWrapperObjectReturnValueAsync"] = "Success",
            ["dotNetToJSReceiveDotNetStreamReferenceAsync"] = "Success",
            ["dotNetToJSReceiveDotNetStreamWrapperReferenceAsync"] = "Success",
            ["jsToDotNetStreamParameterAsync"] = @"""Success""",
            ["jsToDotNetStreamWrapperObjectParameterAsync"] = @"""Success""",
            ["AsyncThrowSyncException"] = @"""System.InvalidOperationException: Threw a sync exception!",
            ["AsyncThrowAsyncException"] = @"""System.InvalidOperationException: Threw an async exception!",
            ["SyncExceptionFromAsyncMethod"] = "Function threw a sync exception!",
            ["AsyncExceptionFromAsyncMethod"] = "Function threw an async exception!",
            ["JSObjectReferenceInvokeNonFunctionException"] = "The value 'nonFunction' is not a function.",
            ["resultReturnDotNetObjectByRefAsync"] = "1001",
            ["instanceMethodThisTypeNameAsync"] = @"""JavaScriptInterop""",
            ["instanceMethodStringValueUpperAsync"] = @"""MY STRING""",
            ["instanceMethodIncomingByRefAsync"] = "123",
            ["instanceMethodOutgoingByRefAsync"] = "1234",
            ["stringValueUpperAsync"] = "MY STRING",
            ["testDtoNonSerializedValueAsync"] = "99999",
            ["testDtoAsync"] = "Same",
            ["returnPrimitiveAsync"] = "123",
            ["returnArrayAsync"] = "first,second",
            ["jsObjectReference.identity"] = "Invoked from JSObjectReference",
            ["jsObjectReference.nested.add"] = "5",
            ["addViaJSObjectReference"] = "5",
            ["jsObjectReferenceModule"] = "Returned from module!",
            ["syncGenericInstanceMethod"] = @"""Initial value""",
            ["asyncGenericInstanceMethod"] = @"""Updated value 1""",
            ["requestDotNetStreamReferenceAsync"] = @"""Success""",
            ["requestDotNetStreamWrapperReferenceAsync"] = @"""Success""",
            ["invokeVoidAsyncReturnsWithoutSerializing"] = "Success",
            ["invokeVoidAsyncReturnsWithoutSerializingInJSObjectReference"] = "Success",
            ["invokeAsyncThrowsSerializingCircularStructure"] = "Success",
            ["invokeAsyncThrowsUndefinedJSObjectReference"] = "Success",
            ["invokeAsyncThrowsNullJSObjectReference"] = "Success",
            ["disposeJSObjectReferenceAsync"] = "Success",
        };

        var expectedSyncValues = new Dictionary<string, string>
        {
            ["VoidParameterless"] = "[]",
            ["VoidWithOneParameter"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
            ["VoidWithTwoParameters"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
            ["VoidWithThreeParameters"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,123]",
            ["VoidWithFourParameters"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,123,16]",
            ["VoidWithFiveParameters"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,123,20,40]",
            ["VoidWithSixParameters"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,123,24,48,6.25]",
            ["VoidWithSevenParameters"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,123,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
            ["VoidWithEightParameters"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,123,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
            ["result1"] = @"[0.1,0.2]",
            ["result2"] = @"[{""id"":1,""isValid"":false,""data"":{""source"":""Some random text with at least 1 characters"",""start"":1,""length"":1}}]",
            ["result3"] = @"[{""id"":2,""isValid"":true,""data"":{""source"":""Some random text with at least 2 characters"",""start"":2,""length"":2}},2]",
            ["result4"] = @"[{""id"":3,""isValid"":false,""data"":{""source"":""Some random text with at least 3 characters"",""start"":3,""length"":3}},3,123]",
            ["result5"] = @"[{""id"":4,""isValid"":true,""data"":{""source"":""Some random text with at least 4 characters"",""start"":4,""length"":4}},4,123,16]",
            ["result6"] = @"[{""id"":5,""isValid"":false,""data"":{""source"":""Some random text with at least 5 characters"",""start"":5,""length"":5}},5,123,20,40]",
            ["result7"] = @"[{""id"":6,""isValid"":true,""data"":{""source"":""Some random text with at least 6 characters"",""start"":6,""length"":6}},6,123,24,48,6.25]",
            ["result8"] = @"[{""id"":7,""isValid"":false,""data"":{""source"":""Some random text with at least 7 characters"",""start"":7,""length"":7}},7,123,28,56,7.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5]]",
            ["result9"] = @"[{""id"":8,""isValid"":true,""data"":{""source"":""Some random text with at least 8 characters"",""start"":8,""length"":8}},8,123,32,64,8.25,[0.5,1.5,2.5,3.5,4.5,5.5,6.5,7.5],{""source"":""Some random text with at least 7 characters"",""start"":9,""length"":9}]",
            ["roundTripJSObjectReference"] = @"""successful""",
            ["invokeDisposedJSObjectReferenceException"] = @"""Error: JS object instance with ID",
            ["ThrowException"] = @"""Threw an exception!",
            ["ExceptionFromSyncMethod"] = "Error: Function threw an exception!",
            ["roundTripByteArrayFromJS"] = @"{""0"":1,""1"":5,""2"":7,""3"":17,""4"":200,""5"":138}",
            ["roundTripByteArrayWrapperObjectFromJS"] = @"{""strVal"":""Some string"",""byteArrayVal"":{""0"":1,""1"":5,""2"":7,""3"":17,""4"":200,""5"":138},""intVal"":42}",
            ["roundTripByteArrayFromDotNet"] = @"1,5,7,15,35,200",
            ["roundTripByteArrayWrapperObjectFromDotNet"] = @"StrVal: Some String, IntVal: 100000, ByteArrayVal: 1,5,7,15,35,200",
            ["resultReturnDotNetObjectByRefSync"] = "1000",
            ["instanceMethodThisTypeName"] = @"""JavaScriptInterop""",
            ["instanceMethodStringValueUpper"] = @"""MY STRING""",
            ["instanceMethodIncomingByRef"] = "123",
            ["instanceMethodOutgoingByRef"] = "1234",
            ["jsInProcessObjectReference.identity"] = "Invoked from JSInProcessObjectReference",
            ["invokeVoidReturnsWithoutSerializingIJSInProcessRuntime"] = "Success",
            ["invokeVoidReturnsWithoutSerializingInIJSInProcessObjectReference"] = "Success",
            ["invokeThrowsSerializingCircularStructure"] = "Success",
            ["invokeThrowsUndefinedJSObjectReference"] = "Success",
            ["invokeThrowsNullJSObjectReference"] = "Success",
            ["stringValueUpperSync"] = "MY STRING",
            ["testDtoNonSerializedValueSync"] = "99999",
            ["testDtoSync"] = "Same",
            ["returnPrimitive"] = "123",
            ["returnArray"] = "first,second",
            ["genericInstanceMethod"] = @"""Updated value 2""",
            ["requestDotNetStreamReference"] = @"""Success""",
            ["requestDotNetStreamWrapperReference"] = @"""Success""",
        };

        // Include the sync assertions only when running under WebAssembly
        var expectedValues = expectedAsyncValues;
        if (_serverFixture.ExecutionMode == ExecutionMode.Client)
        {
            foreach (var kvp in expectedSyncValues)
            {
                expectedValues.Add(kvp.Key, kvp.Value);
            }
        }

        var actualValues = new Dictionary<string, string>();

        // Act
        var interopButton = Browser.Exists(By.Id("btn-interop"));
        interopButton.Click();

        Browser.Exists(By.Id("done-with-interop"));

        foreach (var expectedValue in expectedValues)
        {
            try
            {
                var currentValue = Browser.Exists(By.Id(expectedValue.Key));
                actualValues.Add(expectedValue.Key, currentValue.Text);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to find test id {expectedValue.Key} in DOM.", ex);
            }
        }

        // Assert
        foreach (var expectedValue in expectedValues)
        {
            var actualValue = actualValues[expectedValue.Key];
            if (expectedValue.Key.Contains("Exception"))
            {
                Assert.StartsWith(expectedValue.Value, actualValue);
            }
            else
            {
                if (expectedValue.Value != actualValue)
                {
                    throw new AssertActualExpectedException(expectedValue.Value, actualValue, $"Scenario '{expectedValue.Key}' failed. Expected '{expectedValue.Value}, Actual {actualValue}");
                }
            }
        }
    }
}
