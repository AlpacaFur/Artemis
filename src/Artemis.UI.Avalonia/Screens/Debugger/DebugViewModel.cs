﻿using System;
using System.Reactive.Disposables;
using Artemis.UI.Avalonia.Screens.Debugger.Tabs.DataModel;
using Artemis.UI.Avalonia.Screens.Debugger.Tabs.Logs;
using Artemis.UI.Avalonia.Screens.Debugger.Tabs.Performance;
using Artemis.UI.Avalonia.Screens.Debugger.Tabs.Render;
using Artemis.UI.Avalonia.Services.Interfaces;
using Artemis.UI.Avalonia.Shared;
using FluentAvalonia.UI.Controls;
using Ninject;
using Ninject.Parameters;
using ReactiveUI;

namespace Artemis.UI.Avalonia.Screens.Debugger
{
    public class DebugViewModel : ActivatableViewModelBase, IScreen
    {
        private readonly IKernel _kernel;
        private readonly IDebugService _debugService;
        private bool _isActive;
        private NavigationViewItem? _selectedItem;

        public DebugViewModel(IKernel kernel, IDebugService debugService)
        {
            _kernel = kernel;
            _debugService = debugService;

            this.WhenAnyValue(x => x.SelectedItem).WhereNotNull().Subscribe(NavigateToSelectedItem);
            this.WhenActivated(disposables =>
            {
                Disposable
                    .Create(HandleDeactivation)
                    .DisposeWith(disposables);
            });
        }

        public bool IsActive
        {
            get => _isActive;
            set => this.RaiseAndSetIfChanged(ref _isActive, value);
        }

        public NavigationViewItem? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        private void NavigateToSelectedItem(NavigationViewItem item)
        {
            // Kind of a lame way to do this but it's so static idc
            ConstructorArgument hostScreen = new("hostScreen", this);
            switch ((string) item.Content)
            {
                case "Rendering":
                    Router.Navigate.Execute(_kernel.Get<RenderDebugViewModel>(hostScreen));
                    break;
                case "Logs":
                    Router.Navigate.Execute(_kernel.Get<LogsDebugViewModel>(hostScreen));
                    break;
                case "Data Model":
                    Router.Navigate.Execute(_kernel.Get<DataModelDebugViewModel>(hostScreen));
                    break;
                case "Performance":
                    Router.Navigate.Execute(_kernel.Get<PerformanceDebugViewModel>(hostScreen));
                    break;
            }
        }

        private void HandleDeactivation()
        {
            _debugService.ClearDebugger();
        }

        public RoutingState Router { get; } = new();
    }
}