// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorProjectEngine : RazorProjectEngine
{
    public DefaultRazorProjectEngine(
        RazorConfiguration configuration,
        RazorEngine engine,
        RazorProjectFileSystem fileSystem,
        IReadOnlyList<IRazorProjectEngineFeature> projectFeatures)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        if (engine == null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        if (fileSystem == null)
        {
            throw new ArgumentNullException(nameof(fileSystem));
        }

        if (projectFeatures == null)
        {
            throw new ArgumentNullException(nameof(projectFeatures));
        }

        Configuration = configuration;
        Engine = engine;
        FileSystem = fileSystem;
        ProjectFeatures = projectFeatures;

        for (var i = 0; i < projectFeatures.Count; i++)
        {
            projectFeatures[i].ProjectEngine = this;
        }
    }

    public override RazorConfiguration Configuration { get; }

    public override RazorProjectFileSystem FileSystem { get; }

    public override RazorEngine Engine { get; }

    public override IReadOnlyList<IRazorProjectEngineFeature> ProjectFeatures { get; }

    protected override RazorCodeDocument CreateCodeDocumentCore(RazorProjectItem projectItem)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        return CreateCodeDocumentCore(projectItem, configureParser: null, configureCodeGeneration: null);
    }

    protected RazorCodeDocument CreateCodeDocumentCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptionsBuilder> configureParser,
        Action<RazorCodeGenerationOptionsBuilder> configureCodeGeneration)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

        var importItems = new List<RazorProjectItem>();
        var features = ProjectFeatures.OfType<IImportProjectFeature>();
        foreach (var feature in features)
        {
            importItems.AddRange(feature.GetImports(projectItem));
        }

        var importSourceDocuments = GetImportSourceDocuments(importItems);
        return CreateCodeDocumentCore(sourceDocument, projectItem.FileKind, importSourceDocuments, tagHelpers: null, configureParser, configureCodeGeneration, cssScope: projectItem.CssScope);
    }

    protected internal RazorCodeDocument CreateCodeDocumentCore(
        RazorSourceDocument sourceDocument,
        string fileKind = null,
        IReadOnlyList<RazorSourceDocument> importSourceDocuments = null,
        IReadOnlyList<TagHelperDescriptor> tagHelpers = null,
        Action<RazorParserOptionsBuilder> configureParser = null,
        Action<RazorCodeGenerationOptionsBuilder> configureCodeGeneration = null,
        string cssScope = null)
    {
        if (sourceDocument == null)
        {
            throw new ArgumentNullException(nameof(sourceDocument));
        }

        importSourceDocuments = importSourceDocuments ?? Array.Empty<RazorSourceDocument>();

        var parserOptions = GetRequiredFeature<IRazorParserOptionsFactoryProjectFeature>().Create(fileKind, builder =>
        {
            ConfigureParserOptions(builder);
            configureParser?.Invoke(builder);
        });
        var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactoryProjectFeature>().Create(fileKind, builder =>
        {
            ConfigureCodeGenerationOptions(builder);
            configureCodeGeneration?.Invoke(builder);
        });

        var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
        codeDocument.SetTagHelpers(tagHelpers);

        if (fileKind != null)
        {
            codeDocument.SetFileKind(fileKind);
        }

        if (cssScope != null)
        {
            codeDocument.SetCssScope(cssScope);
        }

        return codeDocument;
    }

    protected override RazorCodeDocument CreateCodeDocumentDesignTimeCore(RazorProjectItem projectItem)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        return CreateCodeDocumentDesignTimeCore(projectItem, configureParser: null, configureCodeGeneration: null);
    }

    protected RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorProjectItem projectItem,
        Action<RazorParserOptionsBuilder> configureParser,
        Action<RazorCodeGenerationOptionsBuilder> configureCodeGeneration)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        var sourceDocument = RazorSourceDocument.ReadFrom(projectItem);

        var importItems = new List<RazorProjectItem>();
        var features = ProjectFeatures.OfType<IImportProjectFeature>();
        foreach (var feature in features)
        {
            importItems.AddRange(feature.GetImports(projectItem));
        }

        var importSourceDocuments = GetImportSourceDocuments(importItems, suppressExceptions: true);
        return CreateCodeDocumentDesignTimeCore(sourceDocument, projectItem.FileKind, importSourceDocuments, tagHelpers: null, configureParser, configureCodeGeneration);
    }

    protected RazorCodeDocument CreateCodeDocumentDesignTimeCore(
        RazorSourceDocument sourceDocument,
        string fileKind,
        IReadOnlyList<RazorSourceDocument> importSourceDocuments,
        IReadOnlyList<TagHelperDescriptor> tagHelpers,
        Action<RazorParserOptionsBuilder> configureParser,
        Action<RazorCodeGenerationOptionsBuilder> configureCodeGeneration)
    {
        if (sourceDocument == null)
        {
            throw new ArgumentNullException(nameof(sourceDocument));
        }

        var parserOptions = GetRequiredFeature<IRazorParserOptionsFactoryProjectFeature>().Create(fileKind, builder =>
        {
            ConfigureDesignTimeParserOptions(builder);
            configureParser?.Invoke(builder);
        });
        var codeGenerationOptions = GetRequiredFeature<IRazorCodeGenerationOptionsFactoryProjectFeature>().Create(fileKind, builder =>
        {
            ConfigureDesignTimeCodeGenerationOptions(builder);
            configureCodeGeneration?.Invoke(builder);
        });

        var codeDocument = RazorCodeDocument.Create(sourceDocument, importSourceDocuments, parserOptions, codeGenerationOptions);
        codeDocument.SetTagHelpers(tagHelpers);

        if (fileKind != null)
        {
            codeDocument.SetFileKind(fileKind);
        }

        return codeDocument;
    }

    public override RazorCodeDocument Process(RazorSourceDocument source, string fileKind, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var codeDocument = CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers, configureParser: null, configureCodeGeneration: null);
        ProcessCore(codeDocument);
        return codeDocument;
    }

    public override RazorCodeDocument ProcessDeclarationOnly(RazorProjectItem projectItem)
    {
        if (projectItem == null)
        {
            throw new ArgumentNullException(nameof(projectItem));
        }

        var codeDocument = CreateCodeDocumentCore(projectItem, configureParser: null, configureCodeGeneration: (builder) =>
        {
            builder.SuppressPrimaryMethodBody = true;
        });

        ProcessCore(codeDocument);
        return codeDocument;
    }

    public override RazorCodeDocument ProcessDeclarationOnly(RazorSourceDocument source, string fileKind, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var codeDocument = CreateCodeDocumentCore(source, fileKind, importSources, tagHelpers, configureParser: null, configureCodeGeneration: (builder) =>
        {
            builder.SuppressPrimaryMethodBody = true;
        });

        ProcessCore(codeDocument);
        return codeDocument;
    }

    public override RazorCodeDocument ProcessDesignTime(RazorSourceDocument source, string fileKind, IReadOnlyList<RazorSourceDocument> importSources, IReadOnlyList<TagHelperDescriptor> tagHelpers)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var codeDocument = CreateCodeDocumentDesignTimeCore(source, fileKind, importSources, tagHelpers, configureParser: null, configureCodeGeneration: null);
        ProcessCore(codeDocument);
        return codeDocument;
    }

    protected override void ProcessCore(RazorCodeDocument codeDocument)
    {
        if (codeDocument == null)
        {
            throw new ArgumentNullException(nameof(codeDocument));
        }

        Engine.Process(codeDocument);
    }

    private TFeature GetRequiredFeature<TFeature>() where TFeature : IRazorProjectEngineFeature
    {
        var feature = ProjectFeatures.OfType<TFeature>().FirstOrDefault();
        if (feature == null)
        {
            throw new InvalidOperationException(
                Resources.FormatRazorProjectEngineMissingFeatureDependency(
                    typeof(RazorProjectEngine).FullName,
                    typeof(TFeature).FullName));
        }

        return feature;
    }

    private void ConfigureParserOptions(RazorParserOptionsBuilder builder)
    {
    }

    private void ConfigureDesignTimeParserOptions(RazorParserOptionsBuilder builder)
    {
        builder.SetDesignTime(true);
    }

    private void ConfigureCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
    {
    }

    private void ConfigureDesignTimeCodeGenerationOptions(RazorCodeGenerationOptionsBuilder builder)
    {
        builder.SetDesignTime(true);
        builder.SuppressChecksum = true;
        builder.SuppressMetadataAttributes = true;
    }

    // Internal for testing
    internal static IReadOnlyList<RazorSourceDocument> GetImportSourceDocuments(
        IReadOnlyList<RazorProjectItem> importItems,
        bool suppressExceptions = false)
    {
        var imports = new List<RazorSourceDocument>();
        for (var i = 0; i < importItems.Count; i++)
        {
            var importItem = importItems[i];

            if (importItem.Exists)
            {
                try
                {
                    // Normal import, has file paths, content etc.
                    var sourceDocument = RazorSourceDocument.ReadFrom(importItem);
                    imports.Add(sourceDocument);
                }
                catch (IOException) when (suppressExceptions)
                {
                    // Something happened when trying to read the item from disk.
                    // Catch the exception so we don't crash the editor.
                }
            }
        }

        return imports;
    }
}
