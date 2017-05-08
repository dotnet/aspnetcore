// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices;
using Microsoft.VisualStudio.LanguageServices.Razor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System.IO;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    internal class RazorInfoViewModel : NotifyPropertyChanged
    {
        private readonly IRazorEngineAssemblyResolver _assemblyResolver;
        private readonly IRazorEngineDirectiveResolver _directiveResolver;
        private readonly IRazorEngineDocumentGenerator _documentGenerator;
        private readonly ITagHelperResolver _tagHelperResolver;
        private readonly IServiceProvider _services;
        private readonly Workspace _workspace;
        private readonly Action<Exception> _errorHandler;

        private DocumentViewModel _currentDocument;
        private DocumentInfoViewModel _currentDocumentInfo;
        private ProjectViewModel _currentProject;
        private ProjectInfoViewModel _currentProjectInfo;
        private ICommand _generateCommand;
        private bool _isLoading;
        private ICommand _loadCommand;

        public RazorInfoViewModel(
            IServiceProvider services,
            Workspace workspace,
            IRazorEngineAssemblyResolver assemblyResolver,
            IRazorEngineDirectiveResolver directiveResolver,
            ITagHelperResolver tagHelperResolver,
            IRazorEngineDocumentGenerator documentGenerator,
            Action<Exception> errorHandler)
        {
            _services = services;
            _workspace = workspace;
            _assemblyResolver = assemblyResolver;
            _directiveResolver = directiveResolver;
            _tagHelperResolver = tagHelperResolver;
            _documentGenerator = documentGenerator;
            _errorHandler = errorHandler;

            GenerateCommand = new RelayCommand<object>(ExecuteGenerate, CanExecuteGenerate);
            LoadCommand = new RelayCommand<object>(ExecuteLoad, CanExecuteLoad);
        }

        public DocumentViewModel CurrentDocument
        {
            get { return _currentDocument; }
            set
            {
                _currentDocument = value;
                OnPropertyChanged();

                CurrentDocumentInfo = null; // Clear cached value
            }
        }

        public DocumentInfoViewModel CurrentDocumentInfo
        {
            get { return _currentDocumentInfo; }
            set
            {
                _currentDocumentInfo = value;
                OnPropertyChanged();
            }
        }

        public ProjectViewModel CurrentProject
        {
            get { return _currentProject; }
            set
            {
                _currentProject = value;
                OnPropertyChanged();

                CurrentProjectInfo = null; // Clear cached value
            }
        }

        public ProjectInfoViewModel CurrentProjectInfo
        {
            get { return _currentProjectInfo; }
            set
            {
                _currentProjectInfo = value;
                OnPropertyChanged();
            }
        }

        public ICommand GenerateCommand
        {
            get { return _generateCommand; }
            set
            {
                _generateCommand = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsSelectionEnabled));
            }
        }

        public bool IsSelectionEnabled => !IsLoading;

        public ICommand LoadCommand
        {
            get { return _loadCommand; }
            set
            {
                _loadCommand = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<ProjectViewModel> Projects { get; } = new ObservableCollection<ProjectViewModel>();

        private bool CanExecuteLoad(object state)
        {
            return !IsLoading && CurrentProject != null;
        }

        private void ExecuteLoad(object state)
        {
            LoadProjectInfoAsync(CurrentProject);
        }

        private bool CanExecuteGenerate(object state)
        {
            return !IsLoading && CurrentDocument != null;
        }

        private void ExecuteGenerate(object state)
        {
            GenerateDocumentAsync(CurrentDocument);
        }

        private async void LoadProjectInfoAsync(ProjectViewModel projectViewModel)
        {
            try
            {
                CurrentProjectInfo = null;
                IsLoading = true;

                var solution = _workspace.CurrentSolution;
                var project = solution.GetProject(projectViewModel.Id);

                var documents = GetCshtmlDocuments(project);
                var assemblies = await _assemblyResolver.GetRazorEngineAssembliesAsync(project);

                var directives = await _directiveResolver.GetRazorEngineDirectivesAsync(_workspace, project);
                var assemblyFilters = project.MetadataReferences
                    .Select(reference => reference.Display)
                    .Select(filter => Path.GetFileNameWithoutExtension(filter));
                var projectFilters = project.AllProjectReferences.Select(filter => solution.GetProject(filter.ProjectId).AssemblyName);
                var resolutionResult = await _tagHelperResolver.GetTagHelpersAsync(project);

                var files = GetCshtmlDocuments(project);

                CurrentProjectInfo = new ProjectInfoViewModel()
                {
                    Assemblies = new ObservableCollection<AssemblyViewModel>(assemblies.Select(a => new AssemblyViewModel(a))),
                    Directives = new ObservableCollection<DirectiveViewModel>(directives.Select(d => new DirectiveViewModel(d))),
                    Documents = new ObservableCollection<DocumentViewModel>(documents.Select(d => new DocumentViewModel(d))),
                    TagHelpers = new ObservableCollection<TagHelperViewModel>(resolutionResult.Descriptors.Select(t => new TagHelperViewModel(t))),
                };
            }
            catch (Exception ex)
            {
                _errorHandler.Invoke(ex);
            }

            IsLoading = false;
        }

        private async void GenerateDocumentAsync(DocumentViewModel documentViewModel)
        {
            try
            {
                CurrentDocumentInfo = null;
                IsLoading = true;

                string text = null;

                var rdt = new RunningDocumentTable(_services);
                var document = rdt.FindDocument(documentViewModel.FilePath);
                if (document != null)
                {
                    text = GetTextFromRDT(document);
                }

                if (text == null)
                {
                    var invisibleEditorMangager = (IVsInvisibleEditorManager)_services.GetService(typeof(SVsInvisibleEditorManager));

                    IVsInvisibleEditor editor;
                    int hr = invisibleEditorMangager.RegisterInvisibleEditor(
                        documentViewModel.FilePath,
                        null,
                        0,
                        null,
                        out editor);
                    Marshal.ThrowExceptionForHR(hr);

                    text = GetTextFromInvisibleEditor(editor);
                }

                if (text != null)
                {
                    var project = _workspace.CurrentSolution.GetProject(CurrentProject.Id);
                    var generated = await _documentGenerator.GenerateDocumentAsync(_workspace, project, documentViewModel.FilePath, text);

                    CurrentDocumentInfo = new DocumentInfoViewModel(generated);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            IsLoading = false;
        }

        private string GetTextFromInvisibleEditor(IVsInvisibleEditor editor)
        {
            int hr = editor.GetDocData(0, typeof(IVsFullTextScanner).GUID, out IntPtr ptr);
            Marshal.ThrowExceptionForHR(hr);

            var fullText = (IVsFullTextScanner)Marshal.GetObjectForIUnknown(ptr);
            Marshal.Release(ptr);

            Marshal.ThrowExceptionForHR(fullText.OpenFullTextScan());
            Marshal.ThrowExceptionForHR(fullText.FullTextRead(out string text, out int length));
            Marshal.ThrowExceptionForHR(fullText.CloseFullTextScan());
            return text;
        }

        private string GetTextFromRDT(object document)
        {
            var fullText = document as IVsFullTextScanner;
            Debug.Assert(fullText != null);

            Marshal.ThrowExceptionForHR(fullText.OpenFullTextScan());
            Marshal.ThrowExceptionForHR(fullText.FullTextRead(out string text, out int length));
            Marshal.ThrowExceptionForHR(fullText.CloseFullTextScan());

            return text;
        }

        private List<string> GetCshtmlDocuments(Project project)
        {
            var workspace = _workspace as VisualStudioWorkspace;
            var hierarchy = workspace.GetHierarchy(project.Id);
            
            var items = new List<string>();
            Traverse(items, hierarchy, (uint)VSConstants.VSITEMID.Root);
            return items;
        }

        private void Traverse(List<string> items, IVsHierarchy node, uint itemId)
        {
            int hr;
            object obj;

            hr = node.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_Name, out obj);
            if (hr == VSConstants.S_OK && obj != null)
            {
                var name = (string)obj;
                if (name.EndsWith(".cshtml"))
                {
                    hr = node.GetCanonicalName(itemId, out name);
                    if (hr == VSConstants.S_OK)
                    {
                        items.Add(name);
                    }
                }
            }

            hr = node.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_FirstChild, out obj);
            if (hr != VSConstants.S_OK || obj == null || (int)obj == unchecked((int)VSConstants.VSITEMID.Nil))
            {
                return;
            }

            itemId = (uint)((int)obj);
            Traverse(items, node, itemId);

            hr = node.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_NextSibling, out obj);
            while (hr == VSConstants.S_OK && obj != null && (int)obj != unchecked((int)VSConstants.VSITEMID.Nil))
            {
                itemId = (uint)((int)obj);
                Traverse(items, node, itemId);

                hr = node.GetProperty(itemId, (int)__VSHPROPID.VSHPROPID_NextSibling, out obj);
            }
        }
    }
}
#endif