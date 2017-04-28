using System;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;

namespace IISSample
{
    public class CurrentResponseTelemetryChannel : ITelemetryChannel
    {
        private readonly HttpResponse _response;

        public CurrentResponseTelemetryChannel(HttpResponse response)
        {
            _response = response;
        }

        public void Dispose()
        {
        }

        public void Send(ITelemetry item)
        {
            if (item is TraceTelemetry traceTelemetry)
            {
                _response.WriteAsync(traceTelemetry.Message + Environment.NewLine).GetAwaiter().GetResult();
            }
        }

        public void Flush()
        {

        }

        public bool? DeveloperMode { get; set; }
        public string EndpointAddress { get; set; }
    }
}