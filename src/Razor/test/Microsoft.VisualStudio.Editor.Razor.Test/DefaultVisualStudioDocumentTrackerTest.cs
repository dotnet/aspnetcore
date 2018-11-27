// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Editor;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;
using Microsoft.VisualStudio.Editor.Razor.Documents;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Moq;
using Xunit;

namespace Microsoft.VisualStudio.Editor.Razor
{
    public class DefaultVisualStudioDocumentTrackerTest : ForegroundDispatcherTestBase
    {
        public DefaultVisualStudioDocumentTrackerTest()
        {
            RazorCoreContentType = Mock.Of<IContentType>(c => c.IsOfType(RazorLanguage.ContentType) == true);
            TextBuffer = Mock.Of<ITextBuffer>(b => b.ContentType == RazorCoreContentType);

            FilePath = "C:/Some/Path/TestDocumentTracker.cshtml";
            ProjectPath = "C:/Some/Path/TestProject.csproj";

            ImportDocumentManager = Mock.Of<ImportDocumentManager>();
            WorkspaceEditorSettings = new DefaultWorkspaceEditorSettings(Mock.Of<ForegroundDispatcher>(), Mock.Of<EditorSettingsManager>());

            TagHelperResolver = new TestTagHelperResolver();
            SomeTagHelpers = new List<TagHelperDescriptor>()
            {
                TagHelperDescriptorBuilder.Create("test", "test").Build(),
            };

            HostServices = TestServices.Create(
                new IWorkspaceService[] { },
                new ILanguageService[] { TagHelperResolver, });

            Workspace = TestWorkspace.Create(HostServices, w =>
            {
                WorkspaceProject = w.AddProject(ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    new VersionStamp(),
                    "Test1",
                    "TestAssembly",
                    LanguageNames.CSharp,
                    filePath: ProjectPath));
            });

            ProjectManager = new TestProjectSnapshotManager(Dispatcher, Workspace) { AllowNotifyListeners = true };

            HostProject = new HostProject(ProjectPath, FallbackRazorConfiguration.MVC_2_1);
            OtherHostProject = new HostProject(ProjectPath, FallbackRazorConfiguration.MVC_2_0);

            DocumentTracker = new DefaultVisualStudioDocumentTracker(
                Dispatcher,
                FilePath,
                ProjectPath,
                ProjectManager,
                WorkspaceEditorSettings,
                Workspace,
                TextBuffer,
                ImportDocumentManager);
        }

        private IContentType RazorCoreContentType { get; }

        private ITextBuffer TextBuffer { get; }

        private string FilePath { get; }

        private string ProjectPath { get; }

        private HostProject HostProject { get; }

        private HostProject OtherHostProject { get; }

        private Project WorkspaceProject { get; set; }

        private ImportDocumentManager ImportDocumentManager { get; }

        private WorkspaceEditorSettings WorkspaceEditorSettings { get; }

        private List<TagHelperDescriptor> SomeTagHelpers { get; }

        private TestTagHelperResolver TagHelperResolver { get; }

        private ProjectSnapshotManagerBase ProjectManager { get; }

        private HostServices HostServices { get; }

        private Workspace Workspace { get; }

        private DefaultVisualStudioDocumentTracker DocumentTracker { get; }

        [ForegroundFact]
        public void Subscribe_NoopsIfAlreadySubscribed()
        {
            // Arrange
            var callCount = 0;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                callCount++;
            };
            DocumentTracker.Subscribe();

            // Call count is 2 right now:
            // 1 trigger for initial subscribe context changed.
            // 1 trigger for TagHelpers being changed (computed).

            // Act
            DocumentTracker.Subscribe();

            // Assert
            Assert.Equal(2, callCount);
        }

        [ForegroundFact]
        public void Unsubscribe_NoopsIfAlreadyUnsubscribed()
        {
            // Arrange
            var callCount = 0;
            DocumentTracker.Subscribe();
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                callCount++;
            };
            DocumentTracker.Unsubscribe();

            // Act
            DocumentTracker.Unsubscribe();

            // Assert
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void Unsubscribe_NoopsIfSubscribeHasBeenCalledMultipleTimes()
        {
            // Arrange
            var callCount = 0;
            DocumentTracker.Subscribe();
            DocumentTracker.Subscribe();
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                callCount++;
            };

            // Act - 1
            DocumentTracker.Unsubscribe();

            // Assert - 1
            Assert.Equal(0, callCount);

            // Act - 2
            DocumentTracker.Unsubscribe();

            // Assert - 2
            Assert.Equal(1, callCount);
        }

        [ForegroundFact]
        public void EditorSettingsManager_Changed_TriggersContextChanged()
        {
            // Arrange
            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                Assert.Equal(ContextChangeKind.EditorSettingsChanged, args.Kind);
                called = true;
                Assert.Equal(ContextChangeKind.EditorSettingsChanged, args.Kind);
            };

            // Act
            DocumentTracker.EditorSettingsManager_Changed(null, null);

            // Assert
            Assert.True(called);
        }

        [ForegroundFact]
        public void ProjectManager_Changed_ProjectAdded_TriggersContextChanged()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);

            var e = new ProjectChangeEventArgs(ProjectPath, ProjectChangeKind.ProjectAdded);

            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                Assert.Equal(ContextChangeKind.ProjectChanged, args.Kind);
                called = true;

                Assert.Same(ProjectManager.GetLoadedProject(DocumentTracker.ProjectPath), DocumentTracker.ProjectSnapshot);
            };

            // Act
            DocumentTracker.ProjectManager_Changed(ProjectManager, e);

            // Assert
            Assert.True(called);
        }

        [ForegroundFact]
        public void ProjectManager_Changed_ProjectChanged_TriggersContextChanged()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);
            ProjectManager.WorkspaceProjectAdded(WorkspaceProject);

            var e = new ProjectChangeEventArgs(ProjectPath, ProjectChangeKind.ProjectChanged);

            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                Assert.Equal(ContextChangeKind.ProjectChanged, args.Kind);
                called = true;

                Assert.Same(ProjectManager.GetLoadedProject(DocumentTracker.ProjectPath), DocumentTracker.ProjectSnapshot);
            };

            // Act
            DocumentTracker.ProjectManager_Changed(ProjectManager, e);

            // Assert
            Assert.True(called);
        }

        [ForegroundFact]
        public void ProjectManager_Changed_ProjectRemoved_TriggersContextChanged_WithEphemeralProject()
        {
            // Arrange
            var e = new ProjectChangeEventArgs(ProjectPath, ProjectChangeKind.ProjectRemoved);

            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                // This can be called both with tag helper and project changes.
                called = true;

                Assert.IsType<EphemeralProjectSnapshot>(DocumentTracker.ProjectSnapshot);
            };

            // Act
            DocumentTracker.ProjectManager_Changed(ProjectManager, e);

            // Assert
            Assert.True(called);
        }

        [ForegroundFact]
        public void ProjectManager_Changed_IgnoresUnknownProject()
        {
            // Arrange
            var e = new ProjectChangeEventArgs("c:/OtherPath/OtherProject.csproj", ProjectChangeKind.ProjectChanged);

            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
            };

            // Act
            DocumentTracker.ProjectManager_Changed(ProjectManager, e);

            // Assert
            Assert.False(called);
        }

        [ForegroundFact]
        public void Import_Changed_ImportAssociatedWithDocument_TriggersContextChanged()
        {
            // Arrange
            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                Assert.Equal(ContextChangeKind.ImportsChanged, args.Kind);
                called = true;
            };

            var importChangedArgs = new ImportChangedEventArgs("path/to/import", FileChangeKind.Changed, new[] { FilePath });

            // Act
            DocumentTracker.Import_Changed(null, importChangedArgs);

            // Assert
            Assert.True(called);
        }

        [ForegroundFact]
        public void Import_Changed_UnrelatedImport_DoesNothing()
        {
            // Arrange
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                throw new InvalidOperationException();
            };

            var importChangedArgs = new ImportChangedEventArgs("path/to/import", FileChangeKind.Changed, new[] { "path/to/differentfile" });

            // Act & Assert (Does not throw)
            DocumentTracker.Import_Changed(null, importChangedArgs);
        }

        [ForegroundFact]
        public void Subscribe_SetsSupportedProjectAndTriggersContextChanged()
        {
            // Arrange
            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                called = true; // This will trigger both ContextChanged and TagHelprsChanged
            };

            // Act
            DocumentTracker.Subscribe();

            // Assert
            Assert.True(called);
            Assert.True(DocumentTracker.IsSupportedProject);
        }

        [ForegroundFact]
        public void Unsubscribe_ResetsSupportedProjectAndTriggersContextChanged()
        {
            // Arrange

            // Subscribe once to set supported project
            DocumentTracker.Subscribe();

            var called = false;
            DocumentTracker.ContextChanged += (sender, args) =>
            {
                called = true;
                Assert.Equal(ContextChangeKind.ProjectChanged, args.Kind);
            };

            // Act
            DocumentTracker.Unsubscribe();

            // Assert
            Assert.False(DocumentTracker.IsSupportedProject);
            Assert.True(called);
        }

        [ForegroundFact]
        public void AddTextView_AddsToTextViewCollection()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();

            // Act
            DocumentTracker.AddTextView(textView);

            // Assert
            Assert.Collection(DocumentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [ForegroundFact]
        public void AddTextView_DoesNotAddDuplicateTextViews()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();

            // Act
            DocumentTracker.AddTextView(textView);
            DocumentTracker.AddTextView(textView);

            // Assert
            Assert.Collection(DocumentTracker.TextViews, v => Assert.Same(v, textView));
        }

        [ForegroundFact]
        public void AddTextView_AddsMultipleTextViewsToCollection()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();

            // Act
            DocumentTracker.AddTextView(textView1);
            DocumentTracker.AddTextView(textView2);

            // Assert
            Assert.Collection(
                DocumentTracker.TextViews,
                v => Assert.Same(v, textView1),
                v => Assert.Same(v, textView2));
        }

        [ForegroundFact]
        public void RemoveTextView_RemovesTextViewFromCollection_SingleItem()
        {
            // Arrange
            var textView = Mock.Of<ITextView>();
            DocumentTracker.AddTextView(textView);

            // Act
            DocumentTracker.RemoveTextView(textView);

            // Assert
            Assert.Empty(DocumentTracker.TextViews);
        }

        [ForegroundFact]
        public void RemoveTextView_RemovesTextViewFromCollection_MultipleItems()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            var textView2 = Mock.Of<ITextView>();
            var textView3 = Mock.Of<ITextView>();
            DocumentTracker.AddTextView(textView1);
            DocumentTracker.AddTextView(textView2);
            DocumentTracker.AddTextView(textView3);

            // Act
            DocumentTracker.RemoveTextView(textView2);

            // Assert
            Assert.Collection(
                DocumentTracker.TextViews,
                v => Assert.Same(v, textView1),
                v => Assert.Same(v, textView3));
        }

        [ForegroundFact]
        public void RemoveTextView_NoopsWhenRemovingTextViewNotInCollection()
        {
            // Arrange
            var textView1 = Mock.Of<ITextView>();
            DocumentTracker.AddTextView(textView1);
            var textView2 = Mock.Of<ITextView>();

            // Act
            DocumentTracker.RemoveTextView(textView2);

            // Assert
            Assert.Collection(DocumentTracker.TextViews, v => Assert.Same(v, textView1));
        }


        [ForegroundFact]
        public void Subscribed_InitializesEphemeralProjectSnapshot()
        {
            // Arrange

            // Act
            DocumentTracker.Subscribe();

            // Assert
            Assert.IsType<EphemeralProjectSnapshot>(DocumentTracker.ProjectSnapshot);
        }

        [ForegroundFact]
        public void Subscribed_InitializesRealProjectSnapshot()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);

            // Act
            DocumentTracker.Subscribe();

            // Assert
            Assert.IsType<DefaultProjectSnapshot>(DocumentTracker.ProjectSnapshot);
        }

        [ForegroundFact]
        public async Task Subscribed_ListensToProjectChanges()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);

            DocumentTracker.Subscribe();

            await DocumentTracker.PendingTagHelperTask;

            // There can be multiple args here because the tag helpers will return
            // immediately and trigger another ContextChanged.
            List<ContextChangeEventArgs> args = new List<ContextChangeEventArgs>();
            DocumentTracker.ContextChanged += (sender, e) => { args.Add(e); };
            
            // Act
            ProjectManager.HostProjectChanged(OtherHostProject);
            await DocumentTracker.PendingTagHelperTask;

            // Assert
            var snapshot = Assert.IsType<DefaultProjectSnapshot>(DocumentTracker.ProjectSnapshot);

            Assert.Same(OtherHostProject, snapshot.HostProject);

            Assert.Collection(
                args,
                e => Assert.Equal(ContextChangeKind.ProjectChanged, e.Kind),
                e => Assert.Equal(ContextChangeKind.TagHelpersChanged, e.Kind));
        }

        [ForegroundFact]
        public async Task Subscribed_ListensToProjectRemoval()
        {
            // Arrange
            ProjectManager.HostProjectAdded(HostProject);

            DocumentTracker.Subscribe();

            await DocumentTracker.PendingTagHelperTask;

            List<ContextChangeEventArgs> args = new List<ContextChangeEventArgs>();
            DocumentTracker.ContextChanged += (sender, e) => { args.Add(e); };

            // Act
            ProjectManager.HostProjectRemoved(HostProject);
            await DocumentTracker.PendingTagHelperTask;

            // Assert
            Assert.IsType<EphemeralProjectSnapshot>(DocumentTracker.ProjectSnapshot);

            Assert.Collection(
                args,
                e => Assert.Equal(ContextChangeKind.ProjectChanged, e.Kind),
                e => Assert.Equal(ContextChangeKind.TagHelpersChanged, e.Kind));
        }

        [ForegroundFact]
        public async Task Subscribed_ListensToProjectChanges_ComputesTagHelpers()
        {
            // Arrange
            TagHelperResolver.CompletionSource = new TaskCompletionSource<TagHelperResolutionResult>();

            ProjectManager.HostProjectAdded(HostProject);

            DocumentTracker.Subscribe();

            // We haven't let the tag helpers complete yet
            Assert.False(DocumentTracker.PendingTagHelperTask.IsCompleted);
            Assert.Empty(DocumentTracker.TagHelpers);

            List<ContextChangeEventArgs> args = new List<ContextChangeEventArgs>();
            DocumentTracker.ContextChanged += (sender, e) => { args.Add(e); };

            // Act
            TagHelperResolver.CompletionSource.SetResult(new TagHelperResolutionResult(SomeTagHelpers, Array.Empty<RazorDiagnostic>()));
            await DocumentTracker.PendingTagHelperTask;

            // Assert
            Assert.Same(DocumentTracker.TagHelpers, SomeTagHelpers);

            Assert.Collection(
                args,
                e => Assert.Equal(ContextChangeKind.TagHelpersChanged, e.Kind));
        }
    }
}
