using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BuildTask = Microsoft.Build.Utilities.Task;

namespace Microsoft.Build.OOB.ESRP
{
    public class ESRPClient : BuildTask
    {
        [Required]
        public string AuthJson
        {
            get;
            set;
        }

        [Required]
        public string ConfigJson
        {
            get;
            set;
        }

        [Required]
        public ITaskItem[] InputJson
        {
            get;
            set;
        }

        [Required]
        public string Path
        {
            get;
            set;
        }

        [Required]
        public string PolicyJson
        {
            get;
            set;
        }

        public int Threshold
        {
            get;
            set;
        } = 200;

        public int Throttle
        {
            get;
            set;
        } = 60000;

        public override bool Execute()
        {
            try
            {
                Process[] ESRPClients = new Process[InputJson.Length];
                int numberOfFiles = 0;
                for (int i = 0; i < InputJson.Length; i++)
                {
                    int numberOfNewFiles = GetNumberOfSignRequests(InputJson[i].ItemSpec);
                    if ((numberOfFiles == 0) || (numberOfFiles + numberOfNewFiles <= Threshold))
                    {
                        ProcessStartInfo pi = new ProcessStartInfo
                        {
                            Arguments = String.Format("sign -a {0} -p {1} -i {2} -o {3} -c {4} -l error", AuthJson, PolicyJson, InputJson[i], InputJson[i].GetMetadata("OutputJson"), ConfigJson),
                            FileName = Path,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                        };

                        Log.LogMessage("Starting ESRPClient for {0} ({1} requests).", InputJson[i], numberOfNewFiles);
                        ESRPClients[i] = Process.Start(pi);
                        numberOfFiles += numberOfNewFiles;
                    }

                    if (numberOfFiles >= Threshold)
                    {
                        Log.LogMessage("Thorttling the services. Additional requests will exceed the threshold: Current/Threshold ({0}/{1})", numberOfFiles, Threshold);
                        Thread.Sleep(Throttle);
                        numberOfFiles = 0;
                    }
                }

                Parallel.For(0, ESRPClients.Length, i =>
                {
                    ESRPClients[i].WaitForExit();
                    Log.LogMessage("ESRPClient for {0} exited with {1}.", InputJson[i], ESRPClients[i].ExitCode);
                    Log.LogMessage(ESRPClients[i].StandardOutput.ReadToEnd());
                });
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
            }

            return !Log.HasLoggedErrors;
        }

        private int GetNumberOfSignRequests(string path)
        {
            using (var sr = new StreamReader(path))
            {
                var json = sr.ReadToEnd();
                var o = JObject.Parse(json).SelectTokens("$.SignBatches[*].SignRequestFiles[*]");
                return o.Count();
            }
        }
    }
}
