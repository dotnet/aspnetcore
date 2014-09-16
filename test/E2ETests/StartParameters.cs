namespace E2ETests
{
    /// <summary>
    /// Summary description for StartParameters
    /// </summary>
    public class StartParameters
    {
        public ServerType ServerType { get; set; }

        public KreFlavor KreFlavor { get; set; }

        public KreArchitecture KreArchitecture { get; set; }

        public string EnvironmentName { get; set; }

        public string ApplicationHostConfigTemplateContent { get; set; }

        public string ApplicationHostConfigLocation { get; set; }

        public string SiteName { get; set; }

        public string ApplicationPath { get; set; }

        public bool PackApplicationBeforeStart { get; set; }

        public string PackedApplicationRootPath { get; set; }
    }
}