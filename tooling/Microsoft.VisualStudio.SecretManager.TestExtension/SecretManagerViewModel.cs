// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.VisualStudio.SecretManager.TestExtension
{
    public class SecretManagerViewModel : NotifyPropertyChanged
    {
        private readonly IProjectService _projectService;
        private readonly Random _rand;
        private string _error;
        private bool _isLoaded;
        private ProjectViewModel _selectedProject;

        public SecretManagerViewModel(IProjectService projectService)
        {
            _projectService = projectService;

            RefreshCommand = new RelayCommand<object>(Refresh, RefreshIsEnabled);
            AddCommand = new RelayCommand<object>(Add, IsProjectLoaded);
            SaveCommand = new RelayCommand<object>(Save, IsProjectLoaded);
            Refresh(null);
            _rand = new Random();
        }

        public RelayCommand<object> RefreshCommand { get; }

        public RelayCommand<object> AddCommand { get; }
        public RelayCommand<object> SaveCommand { get; }

        public ObservableCollection<ProjectViewModel> Projects { get; } = new ObservableCollection<ProjectViewModel>();

        public ProjectViewModel SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (value == _selectedProject)
                {
                    return;
                }

                _selectedProject = value;
                OnSelectedProjectChanged();
                OnPropertyChanged();
            }
        }

        public bool IsLoaded
        {
            get => _isLoaded;
            set
            {
                if (value == _isLoaded)
                {
                    return;
                }

                _isLoaded = value;
                OnPropertyChanged();
            }
        }

        public string Error
        {
            get => _error;
            set
            {
                if (value == _error)
                {
                    return;
                }

                _error = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ErrorVisibility));
            }
        }

        public Visibility ErrorVisibility => Error == null ? Visibility.Collapsed : Visibility.Visible;

        public ObservableCollection<KeyValuePair<string, string>> Secrets { get; } = new ObservableCollection<KeyValuePair<string, string>>();

        private bool RefreshIsEnabled(object obj) => IsLoaded || SelectedProject == null;

        private void Refresh(object obj)
        {
            Projects.Clear();

            foreach (var project in _projectService.LoadedUnconfiguredProjects)
            {
                Projects.Add(new ProjectViewModel(project));
            }
        }

        private bool IsProjectLoaded(object obj) => IsLoaded && SelectedProject != null;

        private void Add(object obj)
        {
            Secrets.Add(new KeyValuePair<string, string>("NewKey" + _rand.Next(10_000), "My new totally random and secret test value"));
        }

        private async void Save(object obj)
        {
            Exception exception;

            try
            {
                IOleServiceProvider oleServices;
                var project = (IVsProject)_selectedProject.Project.Services.HostObject;
                Marshal.ThrowExceptionForHR(project.GetItemContext((uint)VSConstants.VSITEMID.Root, out oleServices));
                var services = new ServiceProvider(oleServices);

                var projectSecrets = (IVsProjectSecrets)services.GetService(typeof(SVsProjectLocalSecrets));
                await TaskScheduler.Default;

                if (projectSecrets == null)
                {
                    exception = null;
                }
                else
                {
                    foreach (var secret in Secrets)
                    {
                        await projectSecrets.SetSecretAsync(secret.Key, secret.Value).ConfigureAwait(false);
                    }

                    exception = null;
                }
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                Error = exception.ToString();
            }
        }

        private async void OnSelectedProjectChanged()
        {
            Secrets.Clear();
            IsLoaded = false;

            if (_selectedProject == null)
            {
                return;
            }

            KeyValuePair<string, string>[] results;
            Exception exception;

            try
            {
                IOleServiceProvider oleServices;
                var project = (IVsProject)_selectedProject.Project.Services.HostObject;
                Marshal.ThrowExceptionForHR(project.GetItemContext((uint)VSConstants.VSITEMID.Root, out oleServices));
                var services = new ServiceProvider(oleServices);

                var projectSecrets = (IVsProjectSecrets)services.GetService(typeof(SVsProjectLocalSecrets));
                await TaskScheduler.Default;

                if (projectSecrets == null)
                {
                    results = null;
                    exception = null;
                }
                else
                {
                    var secrets = await projectSecrets.GetSecretsAsync().ConfigureAwait(false);

                    results = secrets.ToArray();
                    exception = null;
                }
            }
            catch (Exception ex)
            {
                results = null;
                exception = ex;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (exception != null)
            {
                Error = exception.ToString();
            }
            else if (results != null)
            {
                for (var i = 0; i < results.Length; i++)
                {
                    Secrets.Add(results[i]);
                }
            }

            IsLoaded = true;
        }
    }
}
