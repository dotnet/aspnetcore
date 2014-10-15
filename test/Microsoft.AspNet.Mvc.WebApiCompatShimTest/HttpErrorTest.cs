// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http.Formatting;
using Newtonsoft.Json.Linq;
using Xunit;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace System.Web.Http.Dispatcher
{
    public class HttpErrorTest
    {
        public static IEnumerable<object[]> ErrorKeyValue
        {
            get
            {
                var httpError = new HttpError();
                yield return new object[] { httpError, (Func<string>)(() => httpError.Message), "Message", "Message_Value" };
                yield return new object[] { httpError, (Func<string>)(() => httpError.MessageDetail), "MessageDetail", "MessageDetail_Value" };
                yield return new object[] { httpError, (Func<string>)(() => httpError.ExceptionMessage), "ExceptionMessage", "ExceptionMessage_Value" };
                yield return new object[] { httpError, (Func<string>)(() => httpError.ExceptionType), "ExceptionType", "ExceptionType_Value" };
                yield return new object[] { httpError, (Func<string>)(() => httpError.StackTrace), "StackTrace", "StackTrace_Value" };
            }
        }

        public static IEnumerable<object[]> HttpErrors
        {
            get
            {
                yield return new[] { new HttpError() };
                yield return new[] { new HttpError("error") };
                yield return new[] { new HttpError(new NotImplementedException(), true) };
                yield return new[] { new HttpError(new ModelStateDictionary() { { "key", new ModelState() { Errors = { new ModelError("error") } } } }, true) };
            }
        }

        [Fact]
        public void StringConstructor_AddsCorrectDictionaryItems()
        {
            HttpError error = new HttpError("something bad happened");

            Assert.Contains(new KeyValuePair<string, object>("Message", "something bad happened"), error);
        }

        [Fact]
        public void ExceptionConstructorWithDetail_AddsCorrectDictionaryItems()
        {
            HttpError error = new HttpError(new ArgumentException("error", new Exception()), true);

            Assert.Contains(new KeyValuePair<string, object>("Message", "An error has occurred."), error);
            Assert.Contains(new KeyValuePair<string, object>("ExceptionMessage", "error"), error);
            Assert.Contains(new KeyValuePair<string, object>("ExceptionType", "System.ArgumentException"), error);
            Assert.True(error.ContainsKey("StackTrace"));
            Assert.True(error.ContainsKey("InnerException"));
            Assert.IsType<HttpError>(error["InnerException"]);
        }

        [Fact]
        public void ModelStateConstructorWithDetail_AddsCorrectDictionaryItems()
        {
            ModelStateDictionary modelState = new ModelStateDictionary();
            modelState.AddModelError("[0].Name", "error1");
            modelState.AddModelError("[0].Name", "error2");
            modelState.AddModelError("[0].Address", "error");
            modelState.AddModelError("[2].Name", new Exception("OH NO"));

            HttpError error = new HttpError(modelState, true);
            HttpError modelStateError = error["ModelState"] as HttpError;

            Assert.Contains(new KeyValuePair<string, object>("Message", "The request is invalid."), error);
            Assert.Contains("error1", modelStateError["[0].Name"] as IEnumerable<string>);
            Assert.Contains("error2", modelStateError["[0].Name"] as IEnumerable<string>);
            Assert.Contains("error", modelStateError["[0].Address"] as IEnumerable<string>);
            Assert.True(modelStateError.ContainsKey("[2].Name"));
            Assert.Contains("OH NO", modelStateError["[2].Name"] as IEnumerable<string>);
        }

        [Fact]
        public void ExceptionConstructorWithoutDetail_AddsCorrectDictionaryItems()
        {
            HttpError error = new HttpError(new ArgumentException("error", new Exception()), false);

            Assert.Contains(new KeyValuePair<string, object>("Message", "An error has occurred."), error);
            Assert.False(error.ContainsKey("ExceptionMessage"));
            Assert.False(error.ContainsKey("ExceptionType"));
            Assert.False(error.ContainsKey("StackTrace"));
            Assert.False(error.ContainsKey("InnerException"));
        }

        [Fact]
        public void ModelStateConstructorWithoutDetail_AddsCorrectDictionaryItems()
        {
            ModelStateDictionary modelState = new ModelStateDictionary();
            modelState.AddModelError("[0].Name", "error1");
            modelState.AddModelError("[0].Name", "error2");
            modelState.AddModelError("[0].Address", "error");
            modelState.AddModelError("[2].Name", new Exception("OH NO"));

            HttpError error = new HttpError(modelState, false);
            HttpError modelStateError = error["ModelState"] as HttpError;

            Assert.Contains(new KeyValuePair<string, object>("Message", "The request is invalid."), error);
            Assert.Contains("error1", modelStateError["[0].Name"] as IEnumerable<string>);
            Assert.Contains("error2", modelStateError["[0].Name"] as IEnumerable<string>);
            Assert.Contains("error", modelStateError["[0].Address"] as IEnumerable<string>);
            Assert.True(modelStateError.ContainsKey("[2].Name"));
            Assert.DoesNotContain("OH NO", modelStateError["[2].Name"] as IEnumerable<string>);
        }

#if !ASPNETCORE50

        [Fact]
        public void HttpError_Roundtrips_WithJsonFormatter()
        {
            HttpError error = new HttpError("error") { { "ErrorCode", 42 }, { "Data", new[] { "a", "b", "c" } } };
            MediaTypeFormatter formatter = new JsonMediaTypeFormatter();
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(HttpError), error, stream, content: null, transportContext: null).Wait();
            stream.Position = 0;
            HttpError roundtrippedError = formatter.ReadFromStreamAsync(typeof(HttpError), stream, content: null, formatterLogger: null).Result as HttpError;

            Assert.NotNull(roundtrippedError);
            Assert.Equal("error", roundtrippedError.Message);
            Assert.Equal(42L, roundtrippedError["ErrorCode"]);
            JArray data = roundtrippedError["Data"] as JArray;
            Assert.Equal(3, data.Count);
            Assert.Contains("a", data);
            Assert.Contains("b", data);
            Assert.Contains("c", data);
        }

        [Fact]
        public void HttpError_Roundtrips_WithXmlFormatter()
        {
            HttpError error = new HttpError("error") { { "ErrorCode", 42 }, { "Data", new[] { "a", "b", "c" } } };
            MediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(HttpError), error, stream, content: null, transportContext: null).Wait();
            stream.Position = 0;
            HttpError roundtrippedError = formatter.ReadFromStreamAsync(typeof(HttpError), stream, content: null, formatterLogger: null).Result as HttpError;

            Assert.NotNull(roundtrippedError);
            Assert.Equal("error", roundtrippedError.Message);
            Assert.Equal("42", roundtrippedError["ErrorCode"]);
            Assert.Equal("a b c", roundtrippedError["Data"]);
        }

        [Fact]
        public void HttpErrorWithWhitespace_Roundtrips_WithXmlFormatter()
        {
            string message = "  foo\n bar  \n ";
            HttpError error = new HttpError(message);
            MediaTypeFormatter formatter = new XmlMediaTypeFormatter();
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(HttpError), error, stream, content: null, transportContext: null).Wait();
            stream.Position = 0;
            HttpError roundtrippedError = formatter.ReadFromStreamAsync(typeof(HttpError), stream, content: null, formatterLogger: null).Result as HttpError;

            Assert.NotNull(roundtrippedError);
            Assert.Equal(message, roundtrippedError.Message);
        }

        [Fact]
        public void HttpError_Roundtrips_WithXmlSerializer()
        {
            HttpError error = new HttpError("error") { { "ErrorCode", 42 }, { "Data", new[] { "a", "b", "c" } } };
            MediaTypeFormatter formatter = new XmlMediaTypeFormatter() { UseXmlSerializer = true };
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(HttpError), error, stream, content: null, transportContext: null).Wait();
            stream.Position = 0;
            HttpError roundtrippedError = formatter.ReadFromStreamAsync(typeof(HttpError), stream, content: null, formatterLogger: null).Result as HttpError;

            Assert.NotNull(roundtrippedError);
            Assert.Equal("error", roundtrippedError.Message);
            Assert.Equal("42", roundtrippedError["ErrorCode"]);
            Assert.Equal("a b c", roundtrippedError["Data"]);
        }

        [Fact]
        public void HttpErrorForInnerException_Serializes_WithXmlSerializer()
        {
            HttpError error = new HttpError(new ArgumentException("error", new Exception("innerError")), includeErrorDetail: true);
            MediaTypeFormatter formatter = new XmlMediaTypeFormatter() { UseXmlSerializer = true };
            MemoryStream stream = new MemoryStream();

            formatter.WriteToStreamAsync(typeof(HttpError), error, stream, content: null, transportContext: null).Wait();
            stream.Position = 0;
            string serializedError = new StreamReader(stream).ReadToEnd();

            Assert.NotNull(serializedError);
            Assert.Equal(
                "<Error><Message>An error has occurred.</Message><ExceptionMessage>error</ExceptionMessage><ExceptionType>System.ArgumentException</ExceptionType><StackTrace /><InnerException><Message>An error has occurred.</Message><ExceptionMessage>innerError</ExceptionMessage><ExceptionType>System.Exception</ExceptionType><StackTrace /></InnerException></Error>",
                serializedError);
        }

#endif

        [Fact]
        public void GetPropertyValue_GetsValue_IfTypeMatches()
        {
            HttpError error = new HttpError();
            error["key"] = "x";

            Assert.Equal("x", error.GetPropertyValue<string>("key"));
            Assert.Equal("x", error.GetPropertyValue<object>("key"));
        }

        [Fact]
        public void GetPropertyValue_GetsDefault_IfTypeDoesNotMatch()
        {
            HttpError error = new HttpError();
            error["key"] = "x";

            Assert.Null(error.GetPropertyValue<Uri>("key"));
            Assert.Equal(0, error.GetPropertyValue<int>("key"));
        }

        [Fact]
        public void GetPropertyValue_GetsDefault_IfPropertyMissing()
        {
            HttpError error = new HttpError();

            Assert.Null(error.GetPropertyValue<string>("key"));
            Assert.Equal(0, error.GetPropertyValue<int>("key"));
        }

        [Theory]
        [MemberData("ErrorKeyValue")]
        public void HttpErrorStringProperties_UseCorrectHttpErrorKey(HttpError httpError, Func<string> productUnderTest, string key, string actualValue)
        {
            // Arrange
            httpError[key] = actualValue;

            // Act
            string expectedValue = productUnderTest.Invoke();

            // Assert
            Assert.Equal(expectedValue, actualValue);
        }

        [Fact]
        public void HttpErrorProperty_InnerException_UsesCorrectHttpErrorKey()
        {
            // Arrange
            HttpError error = new HttpError(new ArgumentException("error", new Exception()), true);

            // Act
            HttpError innerException = error.InnerException;

            // Assert
            Assert.Same(error["InnerException"], innerException);
        }

        [Fact]
        public void HttpErrorProperty_ModelState_UsesCorrectHttpErrorKey()
        {
            // Arrange
            ModelStateDictionary modelState = new ModelStateDictionary();
            modelState.AddModelError("[0].Name", "error1");
            HttpError error = new HttpError(modelState, true);

            // Act
            HttpError actualModelStateError = error.ModelState;

            // Assert
            Assert.Same(error["ModelState"], actualModelStateError);
        }

        [Theory]
        [MemberData("HttpErrors")]
        public void HttpErrors_UseCaseInsensitiveComparer(HttpError httpError)
        {
            // Arrange
            var lowercaseKey = "abcd";
            var uppercaseKey = "ABCD";

            httpError[lowercaseKey] = "error";

            // Act & Assert
            Assert.True(httpError.ContainsKey(lowercaseKey));
            Assert.True(httpError.ContainsKey(uppercaseKey));
        }
    }
}
