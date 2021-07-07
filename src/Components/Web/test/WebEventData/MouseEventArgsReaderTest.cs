// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Xunit;

namespace Microsoft.AspNetCore.Components.Web
{
    public class MouseEventArgsReaderTest
    {
        [Fact]
        public void Read_Works()
        {
            // Arrange
            var args = new MouseEventArgs
            {
                AltKey = false,
                Button = 72,
                Buttons = 61,
                ClientX = 3.1,
                ClientY = 4.2,
                CtrlKey = true,
                Detail = 7,
                MetaKey = true,
                OffsetX = 8.2,
                OffsetY = 7.1,
                ScreenX = 0.1,
                ScreenY = 4.4,
                ShiftKey = false,
            };
            var jsonElement = GetJsonElement(args);

            // Act
            var result = MouseEventArgsReader.Read(jsonElement);

            // Assert
            AssertEqual(args, result);
        }

        internal static void AssertEqual(MouseEventArgs expected, MouseEventArgs actual)
        {
            Assert.Equal(expected.AltKey, actual.AltKey);
            Assert.Equal(expected.Button, actual.Button);
            Assert.Equal(expected.Buttons, actual.Buttons);
            Assert.Equal(expected.ClientX, actual.ClientX);
            Assert.Equal(expected.ClientY, actual.ClientY);
            Assert.Equal(expected.CtrlKey, actual.CtrlKey);
            Assert.Equal(expected.Detail, actual.Detail);
            Assert.Equal(expected.MetaKey, actual.MetaKey);
            Assert.Equal(expected.OffsetX, actual.OffsetX);
            Assert.Equal(expected.OffsetY, actual.OffsetY);
            Assert.Equal(expected.ScreenX, actual.ScreenX);
            Assert.Equal(expected.ScreenY, actual.ScreenY);
            Assert.Equal(expected.ShiftKey, actual.ShiftKey);
            Assert.Equal(expected.Type, actual.Type);
        }

        private static JsonElement GetJsonElement<T>(T args)
        {
            var json = JsonSerializer.SerializeToUtf8Bytes(args, JsonSerializerOptionsProvider.Options);
            var jsonReader = new Utf8JsonReader(json);
            var jsonElement = JsonElement.ParseValue(ref jsonReader);
            return jsonElement;
        }
    }
}
