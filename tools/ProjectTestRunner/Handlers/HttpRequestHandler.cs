using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;
using ProjectTestRunner.HandlerResults;

namespace ProjectTestRunner.Handlers
{
    public class HttpRequestHandler : IHandler
    {
        public static string Handler => "httpRequest";

        public string HandlerName => Handler;

        public IHandlerResult Execute(IReadOnlyDictionary<string, string> tokens, IReadOnlyList<IHandlerResult> results, JObject json)
        {
            Stopwatch watch = Stopwatch.StartNew();

            try
            {
                string name = json["name"]?.ToString();
                string url = json["url"].ToString();
                int status = json["statusCode"].Value<int>();
                string verb = json["verb"].ToString();
                string body = json["body"]?.ToString();
                string requestMediaType = json["requestMediaType"]?.ToString();
                string requestEncoding = json["requestEncoding"]?.ToString();

                HttpClient client = new HttpClient();
                HttpRequestMessage message = new HttpRequestMessage(new HttpMethod(verb), url);

                if (body != null)
                {
                    if (!string.IsNullOrEmpty(requestEncoding))
                    {
                        if (!string.IsNullOrEmpty(requestMediaType))
                        {
                            message.Content = new StringContent(body, Encoding.GetEncoding(requestEncoding), requestMediaType);
                        }
                        else
                        {
                            message.Content = new StringContent(body, Encoding.GetEncoding(requestEncoding));
                        }
                    }
                    else
                    {
                        message.Content = new StringContent(body);
                    }
                }

                try
                {
                    HttpResponseMessage response = client.SendAsync(message).Result;
                    bool success = status == (int)response.StatusCode;
                    string responseText = response.Content.ReadAsStringAsync().Result;

                    JArray expectations = json["expectations"]?.Value<JArray>();

                    if(expectations != null)
                    {
                        foreach(JObject expectation in expectations.Children().OfType<JObject>())
                        {
                            string assertion = expectation["assertion"]?.Value<string>()?.ToUpperInvariant();
                            string s, key;
                            StringComparison c;
                            IEnumerable<string> values;

                            switch (assertion)
                            {
                                case "RESPONSE_CONTAINS":
                                    s = expectation["text"]?.Value<string>();
                                    if(!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if(responseText.IndexOf(s, c) < 0)
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected output to contain \"{s}\" ({c}), but it did not", name: name);
                                    }

                                    break;
                                case "RESPONSE_DOES_NOT_CONTAIN":
                                    s = expectation["text"]?.Value<string>();
                                    if(!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if(responseText.IndexOf(s, c) > -1)
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected output to NOT contain \"{s}\" ({c}), but it did", name: name);
                                    }

                                    break;
                                case "RESPONSE_HEADER_CONTAINS":
                                    key = expectation["key"]?.Value<string>();
                                    s = expectation["text"]?.Value<string>();
                                    if(!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if(!response.Headers.TryGetValues(key, out values))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to be present, but it was not", name: name);
                                    }

                                    if(!values.Any(x => x.IndexOf(s, c) > -1))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to have a value \"{s}\", but it did not", name: name);
                                    }

                                    break;
                                case "HAS_HEADER":
                                    key = expectation["key"]?.Value<string>();

                                    if(!response.Headers.TryGetValues(key, out values))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to be present, but it was not", name: name);
                                    }

                                    break;

                                case "RESPONSE_HEADER_DOES_NOT_CONTAIN":
                                    key = expectation["key"]?.Value<string>();
                                    s = expectation["text"]?.Value<string>();
                                    if(!Enum.TryParse(expectation["comparison"]?.Value<string>() ?? "OrdinalIgnoreCase", out c))
                                    {
                                        c = StringComparison.OrdinalIgnoreCase;
                                    }

                                    if(!response.Headers.TryGetValues(key, out values))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to be present, but it was not", name: name);
                                    }

                                    if(values.Any(x => x.IndexOf(s, c) > -1))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to NOT have a value \"{s}\", but it did", name: name);
                                    }

                                    break;
                                case "DOES_NOT_HAVE_HEADER":
                                    key = expectation["key"]?.Value<string>();

                                    if(response.Headers.TryGetValues(key, out values))
                                    {
                                        return new ExecuteHandlerResult(watch.Elapsed, false, $"Expected a response header called \"{key}\" to NOT be present, but it was", name: name);
                                    }

                                    break;
                            }
                        }
                    }

                    return new GenericHandlerResult(watch.Elapsed, success, success ? null : $"Expected {status} but got {response.StatusCode}");
                }
                catch (Exception ex)
                {
                    return new GenericHandlerResult(watch.Elapsed, false, ex.Message);
                }
            }
            finally
            {
                watch.Stop();
            }
        }

        public string Summarize(IReadOnlyDictionary<string, string> tokens, JObject json)
        {
            string url = json["url"].ToString();
            int status = json["statusCode"].Value<int>();
            string verb = json["verb"].ToString();
            string body = json["body"]?.ToString();
            string requestMediaType = json["requestMediaType"]?.ToString();
            string requestEncoding = json["requestEncoding"]?.ToString();

            return $"Web Request - {verb} {url} (Body? {body != null}, Encoding? {requestEncoding}, MediaType? {requestMediaType}) -> Expect {status}";
        }
    }
}
