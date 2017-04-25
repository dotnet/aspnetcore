// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;

namespace AspNetCoreModule.Test.Framework
{
    public class TestWebApplication : IDisposable
    {
        private TestWebSite _testSite;
        public TestWebSite TestSite
        {
            get
            {
                return _testSite;
            }
            set
            {
                _testSite = value;
            }
        }

        public TestWebApplication(string name, string physicalPath, string url = null)
            : this(name, physicalPath, null, url)
        {
        }
                
        public TestWebApplication(string name, string physicalPath, TestWebSite siteContext, string url = null)
        {
            _testSite = siteContext;
            _name = name;
            string temp = physicalPath;
            if (physicalPath.Contains("%"))
            {
                temp = System.Environment.ExpandEnvironmentVariables(physicalPath);
            }
            _physicalPath = temp;

            if (url != null)
            {
                _url = url;
            }
            else
            {
                string tempUrl = name.Trim();
                if (tempUrl[0] != '/')
                {
                    _url = "/" + tempUrl;
                }
                else
                {
                    _url = tempUrl;
                }
            }
            BackupFile("web.config");
        }

        public void Dispose()
        {
            DeleteFile("app_offline.htm");
            RestoreFile("web.config");
        }

        private string _name = null;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }

        private string _physicalPath = null;
        public string PhysicalPath
        {
            get
            {
                return _physicalPath;
            }
            set
            {
                _physicalPath = value;
            }
        }

        private string _url = null;
        public string URL
        {
            get
            {
                return _url;
            }
            set
            {
                _url = value;
            }
        }

        public Uri GetUri()
        {
            return new Uri("http://" + _testSite.HostName + ":" + _testSite.TcpPort.ToString() + URL);
        }

        public Uri GetUri(string subPath, int port = -1, string protocol = "http")
        {
            if (port == -1)
            {
                port = _testSite.TcpPort;
            }

            string tempSubPath = string.Empty;
            if (subPath != null)
            {
                tempSubPath = subPath;
                if (!tempSubPath.StartsWith("/"))
                {
                    tempSubPath = "/" + tempSubPath;
                }
            }
            return new Uri(protocol + "://" + _testSite.HostName + ":" + port.ToString() + URL + tempSubPath);
        }
        
        public string _appPoolName = null;
        public string AppPoolName
        {
            get
            {
                if (_appPoolName == null)
                {
                    _appPoolName = "DefaultAppPool";
                }
                return _appPoolName;
            }
            set
            {
                _appPoolName = value;
            }
        }

        public string GetProcessFileName()
        {
            string filePath = Path.Combine(_physicalPath, "web.config");
            string result = null;

            // read web.config
            string fileContent = TestUtility.FileReadAllText(filePath);

            // get the value of processPath attribute of aspNetCore element
            if (fileContent != null)
            {
                result = TestUtility.XmlParser(fileContent, "aspNetCore", "processPath", null);
            }

            // split fileName from full path
            result = Path.GetFileName(result);

            // append .exe if it wasn't used
            if (!result.Contains(".exe"))
            {
                result = result + ".exe";
            }
            return result;
        }

        public string GetArgumentFileName()
        {
            string filePath = Path.Combine(_physicalPath, "web.config");
            string result = null;

            // read web.config
            string fileContent = TestUtility.FileReadAllText(filePath);

            // get the value of arguments attribute of aspNetCore element
            if (fileContent != null)
            {
                result = TestUtility.XmlParser(fileContent, "aspNetCore", "arguments", null);
            }

            // split fileName from full path
            result = Path.GetFileName(result);
            return result;
        }

        public void BackupFile(string from)
        {
            string fromfile = Path.Combine(_physicalPath, from);
            string tofile = Path.Combine(_physicalPath, fromfile + ".bak");
            TestUtility.FileCopy(fromfile, tofile, overWrite: false);
        }

        public void RestoreFile(string from)
        {
            string fromfile = Path.Combine(_physicalPath, from + ".bak");
            string tofile = Path.Combine(_physicalPath, from);
            if (!File.Exists(fromfile))
            {
                BackupFile(from);
            }
            TestUtility.FileCopy(fromfile, tofile);
        }

        public string GetDirectoryPathWith(string subPath)
        {
            return Path.Combine(_physicalPath, subPath);
        }

        public void DeleteFile(string file = "app_offline.htm")
        {
            string filePath = Path.Combine(_physicalPath, file);
            TestUtility.DeleteFile(filePath);
        }

        public void CreateFile(string[] content, string file = "app_offline.htm")
        {
            string filePath = Path.Combine(_physicalPath, file);
            TestUtility.CreateFile(filePath, content);
        }

        public void MoveFile(string from, string to)
        {
            string fromfile = Path.Combine(_physicalPath, from);
            string tofile = Path.Combine(_physicalPath, to);
            TestUtility.FileMove(fromfile, tofile);
        }
        
        public void DeleteDirectory(string directory)
        {
            string directoryPath = Path.Combine(_physicalPath, directory);
            TestUtility.DeleteDirectory(directoryPath);
        }
        
        public void CreateDirectory(string directory)
        {
            string directoryPath = Path.Combine(_physicalPath, directory);
            TestUtility.CreateDirectory(directoryPath);
        }
    }
}