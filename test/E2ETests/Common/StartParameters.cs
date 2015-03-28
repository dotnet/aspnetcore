namespace E2ETests
{
    /// <summary>
    /// Summary description for StartParameters
    /// </summary>
    public class StartParameters
    {
        public ServerType ServerType { get; set; }

        public RuntimeFlavor RuntimeFlavor { get; set; }

        public RuntimeArchitecture RuntimeArchitecture { get; set; }

        public string EnvironmentName { get; set; }

        public string ApplicationHostConfigTemplateContent { get; set; }

        public string ApplicationHostConfigLocation { get; set; }

        public string SiteName { get; set; }

        public string ApplicationPath { get; set; }

        public bool PublishApplicationBeforeStart { get; set; }

        public string PublishedApplicationRootPath { get; set; }

        public bool PublishWithNoSource { get; set; }

        public string Runtime { get; set; }

#if DNX451
        public IISApplication IISApplication { get; set; }
#endif
    }
}
