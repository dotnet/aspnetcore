using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Newtonsoft.Json;

namespace Microsoft.Build.OOB.ESRP
{
    public class CreateSignManifests : Task
    {
        private OperationsJson _operationsJson;

        [Required]
        public string ApplicationId
        {
            get;
            set;
        }

        [Output]
        public string AuthJson
        {
            get;
            set;
        }

        [Output]
        public string ConfigJson
        {
            get;
            set;
        }

        public string DestinationLocationType
        {
            get;
            set;
        }

        [Required]
        public string DestinationRootDirectory
        {
            get;
            set;
        }

        [Required]
        public ITaskItem[] Files
        {
            get;
            set;
        }

        public bool GenerateMultipleInputManifests
        {
            get;
            set;
        }

        [Output]
        public string InputJson
        {
            get;
            set;
        }

        [Output]
        public ITaskItem[] InputJsonManifests
        {
            get;
            set;
        }

        public ITaskItem[] KeyCodes
        {
            get;
            set;
        }

        [Required]
        public string ManifestOutputPath
        {
            get;
            set;
        }

        public int MaxBatchSize
        {
            get;
            set;
        } = 50;

        public string OpusInfo
        {
            get;
            set;
        }

        public string OpusName
        {
            get;
            set;
        }

        [Output]
        public string OutputJson
        {
            get;
            set;
        }

        public OperationsJson OperationsJson
        {
            get
            {
                if (_operationsJson == null)
                {
                    _operationsJson = new OperationsJson(OpusInfo, OpusName);
                }
                return _operationsJson;
            }
        }

        [Output]
        public string PolicyJson
        {
            get;
            set;
        }

        public int SessionTimeout
        {
            get;
            set;
        } = 3600;

        public string SourceLocationType
        {
            get;
            set;
        }

        public string SourceRootDirectory
        {
            get;
            set;
        }

        public bool HasFileKeyCodes
        {
            get
            {
                return Files.Any(o => !String.IsNullOrEmpty(o.GetMetadata("KeyCodes")));
            }
        }

        private void ValidateAndSortKeyCodes()
        {
            // Check whether the keycodes exist, but don't sort because the ordering matters when doing multiple operations, e.g. StrongName + AuthentiCode.
            if (KeyCodes.Count() > 0)
            {
                foreach (var keyCode in KeyCodes)
                {
                    if (!OperationsJson.ContainsKey(keyCode.ItemSpec))
                    {
                        Log.LogError(String.Format("Unknown KeyCode: {0}", keyCode.ItemSpec));
                    }
                }
            }

            // If at least one file has individual KeyCodes metadata we can create batches. Files without key codes are assigned the global key codes.
            // If only some files have the KeyCodes metadata set, but there are no global KeyCodes, then we'll report it as an error.
            if (HasFileKeyCodes)
            {
                // Files can specify individual KeyCodes. Check validity and sort them so we can create batches
                foreach (var f in Files)
                {
                    var fileKeyCodes = f.GetMetadata("KeyCodes");

                    if (String.IsNullOrEmpty(fileKeyCodes))
                    {
                        if (KeyCodes.Count() == 0)
                        {
                            Log.LogError(String.Format("Unable to determine key code for {0}", f.GetMetadata("FullPath")));
                        }
                        else
                        {
                            // Assign the global key codes to the file
                            f.SetMetadata("KeyCodes", String.Join(";", KeyCodes.Select(o => o.ItemSpec)));
                            Log.LogMessage(MessageImportance.Low, String.Format("Setting KeyCodes to for {0} to {1}", f.ItemSpec, f.GetMetadata("KeyCodes")));
                        }
                    }
                    else
                    {
                        // Standardize the individual key codes to reduce the number of batches
                        f.SetMetadata("KeyCodes", String.Join(";", fileKeyCodes));
                        Log.LogMessage(MessageImportance.Low, String.Format("Setting KeyCodes for {0} to {1}", f.ItemSpec, f.GetMetadata("KeyCodes")));
                    }
                }
            }
        }

        public override bool Execute()
        {
            try
            {
                ValidateAndSortKeyCodes();
                GenerateManifests();
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e, showStackTrace: true);
            }

            return !Log.HasLoggedErrors;
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
            AuthJson = Path.Combine(ManifestOutputPath, "auth.json");
            File.WriteAllText(AuthJson,
                JsonConvert.SerializeObject(auth, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void GenerateConfigManifest()
        {
            var config = new Config
            {
                EsrpSessionTimeoutInSec = SessionTimeout
            };

            // Set the output property for the task
            ConfigJson = Path.Combine(ManifestOutputPath, "config.json");

            File.WriteAllText(ConfigJson,
                JsonConvert.SerializeObject(config, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void GeneratePolicyManifest()
        {
            var policy = Policy.Default;

            // Set the output property for the task
            PolicyJson = Path.Combine(ManifestOutputPath, "policy.json");

            File.WriteAllText(PolicyJson,
                JsonConvert.SerializeObject(policy, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));
        }

        private void GenerateInputManifest()
        {
            SignInput input = null;

            if (HasFileKeyCodes)
            {
                input = CreateInputManifestByKeyCode();
            }
            else
            {
                input = CreateSimpleInputManifest();
            }

            // Set the output property for the task
            InputJson = Path.Combine(ManifestOutputPath, "input.json");
            File.WriteAllText(InputJson,
                JsonConvert.SerializeObject(input, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore }));

            // If we're splitting the batches into separate manifests to run multiple clients in parallel, then process the batches a second time
            if (GenerateMultipleInputManifests)
            {
                List<TaskItem> inputJsonFiles = new List<TaskItem>();
                int i = 1;
                foreach (var batch in input.SignBatches)
                {
                    SignInput si = new SignInput { SignBatches = new SignBatches[] { batch } };
                    string inputFile = Path.Combine(ManifestOutputPath, String.Format("input{0}.json", i));
                    string outputFile = Path.Combine(ManifestOutputPath, String.Format("output{0}.json", i));
                    TaskItem ti = new TaskItem(inputFile);
                    ti.SetMetadata("OutputJson", outputFile);
                    inputJsonFiles.Add(ti);
                    File.WriteAllText(inputFile,
                        JsonConvert.SerializeObject(si, Formatting.Indented, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, DefaultValueHandling = DefaultValueHandling.Ignore }));
                    i++;
                }

                InputJsonManifests = inputJsonFiles.ToArray();
            }
        }

        private SignInput CreateInputManifestByKeyCode()
        {
            var si = new SignInput();
            var signBatches = new List<SignBatches>();

            var keyCodeBatches = from f in Files
                                 group f by f.GetMetadata("KeyCodes")
                                 into g
                                 select new
                                 {
                                     KeyCodes = g.Key,
                                     Files = g.ToArray()
                                 };

            foreach (var kcb in keyCodeBatches)
            {
                Log.LogMessage(String.Format("Generating siging batch for KeyCodes: {0}", kcb.KeyCodes));

                var operations = new List<Operations>();

                foreach (var kc in kcb.KeyCodes.Split(';'))
                {
                    var o = OperationsJson[kc];
                    operations.AddRange(o);
                }

                var signRequestFiles = GetSignRequestFiles(kcb.Files);
                int i = 0;
                while (i < signRequestFiles.Count())
                {
                    var batch = signRequestFiles.Skip(i).Take(MaxBatchSize);
                    i += MaxBatchSize;

                    var sb = new SignBatches
                    {
                        DestinationRootDirectory = DestinationRootDirectory,
                        SignRequestFiles = batch.ToArray(),
                        SigningInfo = new SigningInfo
                        {
                            Operations = operations.ToArray()
                        }
                    };

                    signBatches.Add(sb);
                }
            }

            si.SignBatches = signBatches.ToArray();

            return si;
        }

        private SignInput CreateSimpleInputManifest()
        {
            var si = new SignInput();

            var operations = new List<Operations>();

            foreach (var kc in KeyCodes)
            {
                var o = OperationsJson[kc.ItemSpec];
                operations.AddRange(o);
            }

            var signBatches = new List<SignBatches>();
            var signRequestFiles = GetSignRequestFiles(Files);

            int i = 0;
            while (i < signRequestFiles.Count())
            {
                var batch = signRequestFiles.Skip(i).Take(MaxBatchSize);
                i += MaxBatchSize;

                var sb = new SignBatches
                {
                    DestinationRootDirectory = DestinationRootDirectory,
                    SignRequestFiles = batch.ToArray(),
                    SigningInfo = new SigningInfo
                    {
                        Operations = operations.ToArray()
                    }
                };

                signBatches.Add(sb);
            }

            si.SignBatches = signBatches.ToArray();

            return si;
        }

        private IEnumerable<SignRequestFiles> GetSignRequestFiles(ITaskItem[] files)
        {
            var signRequestFiles = new List<SignRequestFiles>();

            foreach (var f in files)
            {
                var srf = new SignRequestFiles
                {
                    SourceLocation = f.GetMetadata("FullPath")
                };

                var fileDestinationLocation = f.GetMetadata("DestinationLocation");
                var fileNameAndExtension = f.GetMetadata("Filename") + f.GetMetadata("Extension");

                if (String.IsNullOrEmpty(fileDestinationLocation))
                {
                    srf.DestinationLocation = fileNameAndExtension;
                }
                else
                {
                    srf.DestinationLocation = Path.Combine(fileDestinationLocation, fileNameAndExtension);
                }

                signRequestFiles.Add(srf);
            };

            return signRequestFiles;
        }

        private void GenerateManifests()
        {
            if (!Directory.Exists(ManifestOutputPath))
            {
                Directory.CreateDirectory(ManifestOutputPath);
            }

            GenerateInputManifest();
            GeneratePolicyManifest();
            GenerateAuthManifest();
            GenerateConfigManifest();

            // ESRPClient.EXE will generate the content, but the build needs to provide a path for this file on the commandline.
            OutputJson = Path.Combine(ManifestOutputPath, "Output.json");
        }
    }
}
