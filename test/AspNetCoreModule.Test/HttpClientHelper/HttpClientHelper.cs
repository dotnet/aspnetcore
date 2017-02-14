// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using AspNetCoreModule.Test.Framework;
using System;
using System.Net;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using System.Net.Sockets;

namespace AspNetCoreModule.Test.HttpClientHelper
{
    public class HttpClientHelper
    {
        private IPHostEntry _host = Dns.GetHostEntry(Dns.GetHostName());
        private string _ipv4Loopback = "127.0.0.1";
        private string _ipv4One = null;
        private string _ipv4Two = null;
        private string _ipv6Loopback = "[::1]";
        private string _ipv6One = null;
        private string _ipv6Two = null;

        public HttpClientHelper()
        {
            ReadMachineIpAddressInfo();

            _Ips = new string[] { _ipv4Loopback, _ipv4One, _ipv6Loopback, _ipv6One, _ipv6Two };

            _Hosts = new string[] { "foo", "bar", "foobar", "barfoo" };

            _unusedIp = _ipv6Two;
        }

        // callback used to validate the certificate in an SSL conversation
        public static bool ValidateRemoteCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors policyErrors)
        {
            return true;
        }

        public static int sendRequest(string uri, string hostName, string expectedCN, bool useLegacy, bool displayContent, bool doLogging = true)
        {
            int status = -1;

            if (doLogging)
            {
                if (hostName == null)
                    TestUtility.LogInformation(String.Format("HttpClient::sendRequest() {0} with no hostname", uri));
                else
                    TestUtility.LogInformation(String.Format("HttpClient::sendRequest() {0} with hostname {1}", uri, hostName));
            }

            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate);

            if (useLegacy)
            {
                TestUtility.LogInformation(String.Format("Using SSL3"));
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
            }

            HttpWebRequest myRequest;
            myRequest = (HttpWebRequest)WebRequest.Create(uri);
            myRequest.Proxy = null;
            myRequest.KeepAlive = false;
            if (hostName != null)
                myRequest.Host = hostName;

            ServicePoint point = myRequest.ServicePoint;
            point.ConnectionLeaseTimeout = 0;

            try
            {
                using (HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse())
                {
                    using (Stream myStream = myResponse.GetResponseStream())
                    {
                        if (displayContent)
                        {
                            using (StreamReader myReader = new StreamReader(myStream))
                            {
                                string text = myReader.ReadToEnd();
                                TestUtility.LogInformation("\n\n");
                                TestUtility.LogInformation(text);
                                TestUtility.LogInformation("\n\n");
                            }
                        }
                    }
                    status = (int)myResponse.StatusCode;
                }
            }
            catch (WebException ex)
            {
                if ((HttpWebResponse)ex.Response == null)
                    status = 0;
                else
                    status = (int)((HttpWebResponse)ex.Response).StatusCode;
            }

            return status;
        }

        public string IPv4Loopback
        {
            get { return _ipv4Loopback; }
        }
        public string IPv4One
        {
            get { return _ipv4One; }
        }
        public string IPv4Two
        {
            get { return _ipv4Two; }
        }
        public string IPv6Loopback
        {
            get { return _ipv6Loopback; }
        }
        public string IPv6One
        {
            get { return _ipv6One; }
        }
        public string IPv6Two
        {
            get { return _ipv6Two; }
        }

        private string[] _Ips;

        private string[] _Hosts = { "foo", "bar", "foobar", "barfoo" };

        private string _unusedIp;


        private Thread _backgroundRequestThread = null;

        public void ReadMachineIpAddressInfo()
        {
            foreach (IPAddress ip in _host.AddressList)
            {
                if (IPAddress.IsLoopback(ip))
                    continue;

                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (_ipv4One == null)
                        _ipv4One = ip.ToString();
                    else if (_ipv4Two == null)
                        _ipv4Two = ip.ToString();
                }
                else if (ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    if (!ip.ToString().Contains("%"))
                    {
                        if (_ipv6One == null)
                            _ipv6One = "[" + ip.ToString() + "]";
                        else if (_ipv6Two == null)
                            _ipv6Two = "[" + ip.ToString() + "]";
                    }
                }
            }
        }

        public int SendReceiveStatus(string path = "/", string protocol = "http", string ip = "127.0.0.1", int port = 8080, string host = "localhost", int expectedStatus = 200, int retryCount = 0)
        {
            string uri = protocol + "://" + ip + ":" + port + path;
            int status = HttpClientHelper.sendRequest(uri, host, "CN=NULL", false, false);
            for (int i = 0; i < retryCount; i++)
            {
                if (status == expectedStatus)
                {
                    break;
                }
                DoSleep(1000);
                status = HttpClientHelper.sendRequest(uri, host, "CN=NULL", false, false);
            }
            return status;
        }

        public void DoRequest(string uri, string host = null, string expectedCN = "CN=NULL", bool useLegacy = false, bool displayContent = false)
        {
            HttpClientHelper.sendRequest(uri, host, expectedCN, useLegacy, displayContent);
        }

        private void BackgroundRequestLoop(object req)
        {
            String[] uriHost = (String[])req;

            while (true)
            {
                HttpClientHelper.sendRequest(uriHost[0], uriHost[1], "CN=NULL", false, false, false);
                Thread.Sleep(5000);
            }
        }

        public void StartBackgroundRequests(string uri, string host = null)
        {
            if (_backgroundRequestThread != null && _backgroundRequestThread.ThreadState == System.Threading.ThreadState.Running)
                _backgroundRequestThread.Abort();

            if (host == null)
                TestUtility.LogInformation(String.Format("########## Starting background requests to {0} with no hostname ##########", uri));
            else
                TestUtility.LogInformation(String.Format("########## Starting background requests to {0} with hostname {1} ##########", uri, host));


            ParameterizedThreadStart threadStart = new ParameterizedThreadStart(BackgroundRequestLoop);
            _backgroundRequestThread = new Thread(threadStart);
            _backgroundRequestThread.IsBackground = true;
            _backgroundRequestThread.Start(new string[] { uri, host });
        }

        public void StopBackgroundRequests()
        {
            TestUtility.LogInformation(String.Format("####################### Stopping background requests #######################"));

            if (_backgroundRequestThread != null && _backgroundRequestThread.ThreadState == System.Threading.ThreadState.Running)
                _backgroundRequestThread.Abort();

            _backgroundRequestThread = null;
        }

        public void DoSleep(int sleepMs)
        {
            TestUtility.LogInformation(String.Format("################## Sleeping for {0} ms ##################", sleepMs));
            Thread.Sleep(sleepMs);
        }
    }

    public class RequestInfo
    {
        public string ip;
        public int port;
        public string host;
        public int status;

        public RequestInfo(string ipIn, int portIn, string hostIn, int statusIn)
        {
            ip = ipIn;
            port = portIn;
            host = hostIn;
            status = statusIn;
        }

        public string ToUrlRegistration()
        {
            if ((ip == null || ip == "*") && (host == null || host == "*"))
                return String.Format("HTTP://*:{0}/", port).ToUpper();

            if (ip == null || ip == "*")
                return String.Format("HTTP://{0}:{1}/", host, port).ToUpper();

            if (host == null || host == "*")
                return String.Format("HTTP://{0}:{1}:{0}/", ip, port).ToUpper();

            return String.Format("HTTP://{0}:{1}:{2}/", host, port, ip).ToUpper();
        }
    }

    public class BindingInfo
    {
        public string ip;
        public int port;
        public string host;

        public BindingInfo(string ip, int port, string host)
        {
            this.ip = ip;
            this.port = port;
            this.host = host;
        }

        public int GetBindingType()
        {
            if (ip == null)
            {
                if (host == null)
                    return 5;
                else
                    return 3;
            }
            else
            {
                if (host == null)
                    return 4;
                else
                    return 2;
            }
        }

        public bool IsSupportedForDynamic()
        {
            return GetBindingType() == 2 || GetBindingType() == 5;
        }

        public bool Match(RequestInfo req)
        {
            if (ip != null && ip != req.ip)
                return false;
            if (port != req.port)
                return false;
            if (host != null && host != req.host)
                return false;

            return true;
        }

        public string ToBindingString()
        {
            string bindingInfoString = "";
            bindingInfoString += (ip == null) ? "*" : ip;
            bindingInfoString += ":";
            bindingInfoString += port;
            bindingInfoString += ":";
            if (host != null)
                bindingInfoString += host;

            return bindingInfoString;
        }

        public string ToUrlRegistration()
        {
            if ((ip == null || ip == "*") && (host == null || host == "*"))
                return String.Format("HTTP://*:{0}/", port).ToUpper();

            if (ip == null || ip == "*")
                return String.Format("HTTP://{0}:{1}/", host, port).ToUpper();

            if (host == null || host == "*")
                return String.Format("HTTP://{0}:{1}:{0}/", ip, port).ToUpper();

            return String.Format("HTTP://{0}:{1}:{2}/", host, port, ip).ToUpper();
        }
    }
}

