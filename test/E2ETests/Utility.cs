using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Xml;

namespace E2ETests
{
    internal class Utility
    {
        public static string GetIISExpressPath()
        {
            var iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "IIS Express", "iisexpress.exe");

            //If X86 version does not exist
            if (!File.Exists(iisExpressPath))
            {
                iisExpressPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "IIS Express", "iisexpress.exe");

                if (!File.Exists(iisExpressPath))
                {
                    throw new Exception("Unable to find IISExpress on the machine");
                }
            }

            return iisExpressPath;
        }

        public static Cookie GetCookieWithName(CookieCollection cookieCollection, string cookieName)
        {
            foreach (Cookie cookie in cookieCollection)
            {
                if (cookie.Name == cookieName)
                {
                    return cookie;
                }
            }

            return null;
        }

        public static string RetrieveAntiForgeryToken(string htmlContent, string actionUrl)
        {
            int startSearchIndex = 0;

            while (startSearchIndex < htmlContent.Length)
            {
                var antiForgeryToken = RetrieveAntiForgeryToken(htmlContent, actionUrl, ref startSearchIndex);

                if (antiForgeryToken != null)
                {
                    return antiForgeryToken;
                }
            }

            return string.Empty;
        }

        private static string RetrieveAntiForgeryToken(string htmlContent, string actionLocation, ref int startIndex)
        {
            var formStartIndex = htmlContent.IndexOf("<form", startIndex, StringComparison.OrdinalIgnoreCase);
            var formEndIndex = htmlContent.IndexOf("</form>", startIndex, StringComparison.OrdinalIgnoreCase);

            if (formStartIndex == -1 || formEndIndex == -1)
            {
                //Unable to find the form start or end - finish the search
                startIndex = htmlContent.Length;
                return null;
            }

            formEndIndex = formEndIndex + "</form>".Length;
            startIndex = formEndIndex + 1;

            var htmlDocument = new XmlDocument();
            htmlDocument.LoadXml(htmlContent.Substring(formStartIndex, formEndIndex - formStartIndex));

            foreach (XmlAttribute attribute in htmlDocument.DocumentElement.Attributes)
            {
                if (string.Compare(attribute.Name, "action", true) == 0 && attribute.Value.EndsWith(actionLocation, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (XmlNode input in htmlDocument.GetElementsByTagName("input"))
                    {
                        if (input.Attributes["name"].Value == "__RequestVerificationToken" && input.Attributes["type"].Value == "hidden")
                        {
                            return input.Attributes["value"].Value;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Copy AspNet.Loader.dll to bin folder
        /// </summary>
        /// <param name="applicationPath"></param>
        public static void CopyAspNetLoader(string applicationPath)
        {
            string packagesDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, @"..\..\Packages"));
            var aspNetLoaderSrcPath = Path.Combine(Directory.GetDirectories(packagesDirectory, "Microsoft.AspNet.Loader.IIS.Interop.*").First(), @"tools\AspNet.Loader.dll");
            var aspNetLoaderDestPath = Path.Combine(applicationPath, @"bin\AspNet.Loader.dll");
            if (!File.Exists(aspNetLoaderDestPath))
            {
                File.Copy(aspNetLoaderSrcPath, aspNetLoaderDestPath);
            }
        }

        public static Process StartHeliosHost(string applicationPath)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = Utility.GetIISExpressPath(),
                Arguments = string.Format("/port:5001 /path:{0}", applicationPath),
                UseShellExecute = true,
                CreateNoWindow = true
            };

            var hostProcess = Process.Start(startInfo);
            Console.WriteLine("Started iisexpress. Process Id : {0}", hostProcess.Id);
            Thread.Sleep(2 * 1000);

            return hostProcess;
        }
    }
}