using System.Collections.Generic;

namespace AspNetCoreSdkTests.Templates
{
    public abstract class Template
    {
        public abstract string Name { get; }
        public abstract TemplateType Type { get; }
        public virtual string RelativeUrl => string.Empty;

        public virtual IEnumerable<string> ExpectedObjFilesAfterRestore => new[]
        {
            $"{Name}.csproj.nuget.cache",
            $"{Name}.csproj.nuget.g.props",
            $"{Name}.csproj.nuget.g.targets",
            "project.assets.json",
        };

        public virtual IEnumerable<string> ExpectedObjFilesAfterBuild => ExpectedObjFilesAfterRestore;

        public abstract IEnumerable<string> ExpectedBinFilesAfterBuild { get; }

        public abstract IEnumerable<string> ExpectedFilesAfterPublish { get; }

        public override string ToString() => Name;
    }
}
