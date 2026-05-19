// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Google.Protobuf;

namespace SocialWeather.Protobuf;

public class ProtobufWeatherStreamFormatter : IStreamFormatter<SocialWeather.WeatherReport>
{
    public Task<SocialWeather.WeatherReport> ReadAsync(Stream stream)
    {
        var inputStream = new CodedInputStream(stream, leaveOpen: true);
        var protoWeatherReport = new Protobuf.WeatherReport();
        inputStream.ReadMessage(protoWeatherReport);
        return Task.FromResult(new SocialWeather.WeatherReport
        {
            Temperature = protoWeatherReport.Temperature,
            ReportTime = protoWeatherReport.ReportTime,
            Weather = (Weather)(int)protoWeatherReport.Weather,
            ZipCode = protoWeatherReport.ZipCode
        });
    }

    public async Task WriteAsync(SocialWeather.WeatherReport weatherReport, Stream stream)
    {
        var outputStream = new CodedOutputStream(stream, leaveOpen: true);
        var protoWeatherReport = new Protobuf.WeatherReport
        {
            Temperature = weatherReport.Temperature,
            ReportTime = weatherReport.ReportTime,
            Weather = (Protobuf.WeatherReport.Types.WeatherKind)(int)weatherReport.Weather,
            ZipCode = weatherReport.ZipCode
        };

        outputStream.WriteMessage(protoWeatherReport);
        outputStream.Flush();
        await stream.FlushAsync();
    }
}
