using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Build.Framework;
using Microsoft.Build.OOB.ESRP;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace MicroBuild.Plugins.TeamCity.Signing
{
    public class SignFiles : ToolTask
    {
        public SignFiles()
        {
            LogStandardErrorAsError = false;
            YieldDuringToolExecution = true;
        }

        private string _manifestOutputPath;
        private string _configJson;
        private string _inputJson;
        private string _authJson;
        private string _policyJson;
        private string _outputJson;
        private string _logFile;

        /// <summary>
        /// A list of files and certificates.
        /// </summary>
        [Required]
        public ITaskItem[] Files { get; set; }

        /// <summary>
        /// The ESRP application ID used to authenticate to the ESRP service.
        /// </summary>
        [Required]
        public string ApplicationId { get; set; }

        [Required]
        public string Type { get; set; }

        [Required]
        public string BinariesDirectory { get; set; }

        [Required]
        public string IntermediatesDirectory { get; set; }

        public string JobName { get; set; }

        public string LogOutputDir { get; set; }

        public int MaxBatchSize { get; set; } = 50;

        // increase the default output importance from Low to Normal
        protected override MessageImportance StandardOutputLoggingImportance { get; } = MessageImportance.Normal;

        protected override MessageImportance StandardErrorLoggingImportance { get; } = MessageImportance.Normal;

        protected override string ToolName => "ESRPClient.exe";

        public override bool Execute()
        {
            if (!string.Equals(Type, "real", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogError("This task only implements real signing. SignType={0} is an invalid value", Type);
                return false;
            }

            if (Files == null || Files.Length == 0)
            {
                return true;
            }

            JobName = string.IsNullOrEmpty(this.JobName)
                ? Guid.NewGuid().ToString()
                : this.JobName;

            Log.LogMessage(MessageImportance.High, "Starting code sign job {0}", JobName);

            BinariesDirectory = Path.GetFullPath(BinariesDirectory);
            IntermediatesDirectory = Path.GetFullPath(IntermediatesDirectory);

            _manifestOutputPath = Path.Combine(IntermediatesDirectory, JobName);
            _logFile = string.IsNullOrEmpty(LogOutputDir)
                ? Path.Combine(_manifestOutputPath, "log.txt")
                : Path.Combine(LogOutputDir, $"signjob-{JobName}.txt");

            Directory.CreateDirectory(BinariesDirectory);
            Directory.CreateDirectory(IntermediatesDirectory);
            Directory.CreateDirectory(_manifestOutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(_logFile));

            GenerateManifests();
            Log.LogMessage(MessageImportance.High, "Code signing log files for job '{0}' will be created in '{1}'", JobName, _logFile);
            Log.LogMessage("Generated manifests in {0}", _manifestOutputPath);

            var retVal = base.Execute();
            Log.LogMessage(MessageImportance.High, "Finished code sign job {0}", JobName);

            return retVal && !Log.HasLoggedErrors;
        }

        protected override string GenerateFullPathToTool() => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(typeof(SignFiles).Assembly.Location), "..", "ESRPClient", "ESRPClient.exe"));

        protected override string GenerateCommandLineCommands()
        {
            var sb = new StringBuilder();
            sb.Append("sign ");

            sb.Append("-l progress ");
            sb.Append("-c \"").Append(_configJson).Append("\" ");
            sb.Append("-a \"").Append(_authJson).Append("\" ");
            sb.Append("-p \"").Append(_policyJson).Append("\" ");
            sb.Append("-i \"").Append(_inputJson).Append("\" ");
            sb.Append("-o \"").Append(_outputJson).Append("\" ");
            sb.Append("-f \"").Append(_logFile).Append("\" ");

            return sb.ToString();
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            // Ignore standard error and warning formatting
            Log.LogMessage(messageImportance, singleLine);
        }

        protected override bool ValidateParameters()
        {
            var esrpClient = GenerateFullPathToTool();

            if (!File.Exists(esrpClient))
            {
                Log.LogError("Could not find ESRPClient. Expected it to exist in {0}", esrpClient);
                return false;
            }

            return true;
        }

        private void GenerateAuthManifest()
        {
            var auth = new Auth
            {
                Version = "1.0.0",
                AuthenticationType = "AAD_CERT",
                ClientId = ApplicationId,
                AuthCert = AuthCert.Create(ApplicationId),
                RequestSigningCert = RequestSigningCert.Create(ApplicationId)
            };

            // Set the output property for the task
            _authJson = Path.Combine(_manifestOutputPath, "auth.json");
            File.WriteAllText(_authJson,
                JsonConvert.SerializeObject(auth, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void GeneratePolicyManifest()
        {
            var policy = Policy.Default;

            // Set the output property for the task
            _policyJson = Path.Combine(_manifestOutputPath, "policy.json");

            File.WriteAllText(_policyJson,
                JsonConvert.SerializeObject(policy, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void GenerateInputManifest()
        {
            var input = CreateInputManifestByKeyCode();

            // Set the output property for the task
            _inputJson = Path.Combine(_manifestOutputPath, "input.json");
            File.WriteAllText(_inputJson,
                JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore }));
        }

        private SignInput CreateInputManifestByKeyCode()
        {
            var si = new SignInput();
            var signBatches = new List<SignBatches>();

            var keyCodeBatches = from f in Files
                                 group f by f.GetMetadata("Authenticode")
                                 into g
                                 select new
                                 {
                                     CertName = g.Key,
                                     Files = g.ToArray()
                                 };

            foreach (var kcb in keyCodeBatches)
            {
                Log.LogMessage(string.Format("Generating signing batch for certificate: {0}", kcb.CertName));

                var operations = CertificateNameMapping.GetOperations(kcb.CertName);

                foreach (var signRequestFiles in GetSignRequestFiles(kcb.Files).Batch(MaxBatchSize))
                {
                    var sb = new SignBatches
                    {
                        DestinationRootDirectory = BinariesDirectory,
                        SignRequestFiles = signRequestFiles.ToArray(),
                        SigningInfo = new SigningInfo
                        {
                            Operations = operations
                        }
                    };

                    signBatches.Add(sb);
                }
            }

            si.SignBatches = signBatches.ToArray();

            return si;
        }

        private IEnumerable<SignRequestFiles> GetSignRequestFiles(ITaskItem[] files)
        {
            foreach (var f in files)
            {
                yield return new SignRequestFiles
                {
                    SourceLocation = f.ItemSpec
                };
            };
        }

        private void GenerateConfigManifest()
        {
            _configJson = Path.Combine(_manifestOutputPath, "config.json");
            File.WriteAllText(_configJson,
                JsonConvert.SerializeObject(new
                {
                    Version = "1.0.0",
                    EsrpSessionTimeoutInSec = 7200,
                },
                Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore }));
        }

        private void GenerateManifests()
        {
            if (!Directory.Exists(_manifestOutputPath))
            {
                Directory.CreateDirectory(_manifestOutputPath);
            }

            GenerateConfigManifest();
            GenerateInputManifest();
            GeneratePolicyManifest();
            GenerateAuthManifest();

            // ESRPClient.EXE will generate the content, but the build needs to provide a path for this file on the commandline.
            _outputJson = Path.Combine(_manifestOutputPath, "output.json");
        }
    }
}
