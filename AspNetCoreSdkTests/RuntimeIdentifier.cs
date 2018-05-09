namespace AspNetCoreSdkTests
{
    // https://docs.microsoft.com/en-us/dotnet/core/rid-catalog
    public class RuntimeIdentifier
    {
        public static RuntimeIdentifier None = new RuntimeIdentifier() { Name = "none" };
        public static RuntimeIdentifier Win_x64 = new RuntimeIdentifier() { Name = "win-x64" };

        private RuntimeIdentifier() { }

        public string Name { get; private set; }
        public string RuntimeArgument => (this == None) ? string.Empty : $"--runtime {Name}";
        public string Path => (this == None) ? string.Empty : Name;

        public override string ToString() => Name;
    }
}
