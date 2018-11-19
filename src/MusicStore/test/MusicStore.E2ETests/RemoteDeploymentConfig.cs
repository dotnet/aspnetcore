namespace E2ETests
{
    public class RemoteDeploymentConfig
    {
        /// <summary>
        /// Name or IP address of the server to deploy to
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Account name for the credentials required to setup a powershell session to the target server
        /// </summary>
        public string AccountName { get; set; }

        /// <summary>
        /// Account password for the credentials required to setup a powershell session to the target server
        /// </summary>
        public string AccountPassword { get; set; }

        /// <summary>
        /// The file share on the target server to copy the files to
        /// </summary>
        public string FileSharePath { get; set; }

        /// <summary>
        /// Location of the dotnet runtime zip file which is required for testing portable apps.
        /// When both <see cref="DotnetRuntimeZipFilePath"/> and <see cref="DotnetRuntimeFolderPath"/> properties
        /// are provided, the <see cref="DotnetRuntimeZipFilePath"/> property is preferred.
        /// </summary>
        public string DotnetRuntimeZipFilePath { get; set; }

        /// <summary>
        /// Location of the dotnet runtime folder which is required for testing portable apps.
        /// This property is probably more useful for users as they can point their local 'dotnet' runtime folder.
        /// </summary>
        public string DotnetRuntimeFolderPath { get; set; }

        /// <summary>
        /// Path to the parent folder containing 'dotnet.exe' on remote server's file share
        /// </summary>
        public string DotnetRuntimePathOnShare { get; set; }
    }
}
