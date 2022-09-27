// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.Extensions.Tools.Internal;

public class TemporaryOpenApiProject : TemporaryCSharpProject
{
    public TemporaryOpenApiProject(string name, TemporaryDirectory directory, string sdk)
        : base(name, directory, sdk)
    {
    }

    protected override string Template =>
@"<Project Sdk=""{2}"">
  <Import Project=""build/Microsoft.Extensions.ApiDescription.Client.props"" />

  <PropertyGroup>
    {0}
  </PropertyGroup>
  <ItemGroup>
    {1}
  </ItemGroup>

  <!-- Check _CreateCompileItemsForOpenApiReferences output items. -->
  <Target Name=""_WriteWrites"" AfterTargets=""_CreateCompileItemsForOpenApiReferences"">
    <Message Importance=""high""
        Text=""Compile: %(Compile.Identity)""
        Condition="" '@(Compile)' != '' "" />
    <Message Importance=""high"" Text=""FileWrites: %(FileWrites.Identity)"" />
    <Message Importance=""high""
        Text=""TypeScriptCompile: %(TypeScriptCompile.Identity)""
        Condition="" '@(TypeScriptCompile)' != '' "" />
  </Target>

  <Import Project=""build/Microsoft.Extensions.ApiDescription.Client.targets"" />
  <Import Project=""build/Fakes.targets"" />
</Project>";

    protected override void AddAdditionalAttributes(StringBuilder sb, TemporaryCSharpProject.ItemSpec item)
    {
        var openApiItem = item as ItemSpec;
        if (openApiItem.ClassName != null)
        {
            sb.Append(" ClassName=\"").Append(openApiItem.ClassName).Append('"');
        }

        if (openApiItem.CodeGenerator != null)
        {
            sb.Append(" CodeGenerator=\"").Append(openApiItem.CodeGenerator).Append('"');
        }

        if (openApiItem.Namespace != null)
        {
            sb.Append(" Namespace=\"").Append(openApiItem.Namespace).Append('"');
        }

        if (openApiItem.Options != null)
        {
            sb.Append(" Options=\"").Append(openApiItem.Options).Append('"');
        }

        if (openApiItem.OutputPath != null)
        {
            sb.Append(" OutputPath=\"").Append(openApiItem.OutputPath).Append('"');
        }
    }

    public new class ItemSpec : TemporaryCSharpProject.ItemSpec
    {
        public ItemSpec() : base()
        {
            Name = "OpenApiReference";
        }

        // Metadata specific to OpenApiReference items.
        public string ClassName { get; set; }

        public string CodeGenerator { get; set; }

        public string Namespace { get; set; }

        public string Options { get; set; }

        public string OutputPath { get; set; }
    }
}
