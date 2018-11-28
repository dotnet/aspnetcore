// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using Google.Protobuf;

namespace SocialWeather.Protobuf
{
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
}
