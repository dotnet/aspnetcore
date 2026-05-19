// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

public class DefaultTempDataSerializerTest : TempDataSerializerTestBase
{
    protected override TempDataSerializer GetTempDataSerializer() => new DefaultTempDataSerializer();

    [Fact]
    public void RoundTripTest_NonStandardDateTimeStringFormat_RoundTripsAsString()
    {
        // DateTime that do not match the format that System.Text.Json uses for round-tripping
        // should round-trip as strings.

        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new DateTime(2009, 1, 1, 12, 37, 43);
        var input = new Dictionary<string, object>
            {
                { key, value.ToString("r") }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string>(values[key]);
        Assert.Equal(value.ToString("r"), roundTripValue);
    }

    [Fact]
    public override void RoundTripTest_DictionaryOfInt()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new Dictionary<string, int>
            {
                { "Key1", 7 },
                { "Key2", 24 },
            };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => testProvider.Serialize(input));
        Assert.Equal($"The '{testProvider.GetType()}' cannot serialize an object of type '{value.GetType()}'.", ex.Message);
    }
}
