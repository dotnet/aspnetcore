// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if RAZOR_EXTENSION_DEVELOPER_MODE

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.VisualStudio.RazorExtension.RazorInfo
{
    public class TagHelperCollectionViewModel : NotifyPropertyChanged
    {
        private readonly ProjectSnapshot _project;
        private readonly Action<Exception> _errorHandler;

        private Visibility _progressVisibility;

        internal TagHelperCollectionViewModel(ProjectSnapshot project, Action<Exception> errorHandler)
        {
            _project = project;
            _errorHandler = errorHandler;

            TagHelpers = new ObservableCollection<TagHelperItemViewModel>();
            InitializeTagHelpers();
        }

        public ObservableCollection<TagHelperItemViewModel> TagHelpers { get; }

        public Visibility ProgressVisibility
        {
            get => _progressVisibility;
            set
            {
                _progressVisibility = value;
                OnPropertyChanged();
            }
        }

        private async void InitializeTagHelpers()
        {
            ProgressVisibility = Visibility.Hidden;

            try
            {
                if (!_project.TryGetTagHelpers(out var tagHelpers))
                {
                    ProgressVisibility = Visibility.Visible;
                    tagHelpers = await _project.GetTagHelpersAsync();
                    await Task.Delay(250); // Force a delay for the UI
                }

                foreach (var tagHelper in tagHelpers)
                {
                    TagHelpers.Add(new TagHelperItemViewModel(tagHelper));
                }
            }
            catch (Exception ex)
            {
                _errorHandler(ex);
            }
            finally
            {
                ProgressVisibility = Visibility.Hidden;
            }
        }
    }
}

#endif
