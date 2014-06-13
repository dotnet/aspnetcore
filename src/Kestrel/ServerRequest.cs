using Microsoft.AspNet.HttpFeature;
using Microsoft.AspNet.Server.Kestrel.Http;
using System;
using System.Collections.Generic;
using System.IO;

namespace Kestrel
{
    public class ServerRequest : IHttpRequestFeature, IHttpResponseFeature
    {
        Frame _frame;
        string _scheme;
        string _pathBase;

        public ServerRequest(Frame frame)
        {
            _frame = frame;
        }

        string IHttpRequestFeature.Protocol
        {
            get
            {
                return _frame.HttpVersion;
            }

            set
            {
                _frame.HttpVersion = value;
            }
        }

        string IHttpRequestFeature.Scheme
        {
            get
            {
                return _scheme ?? "http";
            }

            set
            {
                _scheme = value;
            }
        }

        string IHttpRequestFeature.Method
        {
            get
            {
                return _frame.Method;
            }

            set
            {
                _frame.Method = value;
            }
        }

        string IHttpRequestFeature.PathBase
        {
            get
            {
                return _pathBase ?? "";
            }

            set
            {
                _pathBase = value;
            }
        }

        string IHttpRequestFeature.Path
        {
            get
            {
                return _frame.Path;
            }

            set
            {
                _frame.Path = value;
            }
        }

        string IHttpRequestFeature.QueryString
        {
            get
            {
                return _frame.QueryString;
            }

            set
            {
                _frame.QueryString = value;
            }
        }

        IDictionary<string, string[]> IHttpRequestFeature.Headers
        {
            get
            {
                return _frame.RequestHeaders;
            }

            set
            {
                _frame.RequestHeaders = value;
            }
        }

        Stream IHttpRequestFeature.Body
        {
            get
            {
                return _frame.RequestBody;
            }

            set
            {
                _frame.RequestBody = value;
            }
        }

        int IHttpResponseFeature.StatusCode
        {
            get
            {
                return _frame.StatusCode;
            }

            set
            {
                _frame.StatusCode = value;
            }
        }

        string IHttpResponseFeature.ReasonPhrase
        {
            get
            {
                return _frame.ReasonPhrase;
            }

            set
            {
                _frame.ReasonPhrase = value;
            }
        }

        IDictionary<string, string[]> IHttpResponseFeature.Headers
        {
            get
            {
                return _frame.ResponseHeaders;
            }

            set
            {
                _frame.ResponseHeaders = value;
            }
        }

        Stream IHttpResponseFeature.Body
        {
            get
            {
                return _frame.ResponseBody;
            }

            set
            {
                _frame.ResponseBody = value;
            }
        }
        void IHttpResponseFeature.OnSendingHeaders(Action<object> callback, object state)
        {
            _frame.OnSendingHeaders(callback, state);
        }
    }
}
