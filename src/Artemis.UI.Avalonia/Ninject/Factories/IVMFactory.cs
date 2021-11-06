﻿using System.Collections.ObjectModel;
using Artemis.Core;
using Artemis.UI.Avalonia.Screens.Device;
using Artemis.UI.Avalonia.Screens.Device.Tabs.ViewModels;
using Artemis.UI.Avalonia.Screens.Root.ViewModels;
using Artemis.UI.Avalonia.Screens.SurfaceEditor.ViewModels;
using ReactiveUI;

namespace Artemis.UI.Avalonia.Ninject.Factories
{
    public interface IVmFactory
    {
    }

    public interface IDeviceVmFactory : IVmFactory
    {
        DevicePropertiesViewModel DevicePropertiesViewModel(ArtemisDevice device);
        DevicePropertiesTabViewModel DevicePropertiesTabViewModel(ArtemisDevice device);
        DeviceInfoTabViewModel DeviceInfoTabViewModel(ArtemisDevice device);
        DeviceLedsTabViewModel DeviceLedsTabViewModel(ArtemisDevice device, ObservableCollection<ArtemisLed> selectedLeds);
        InputMappingsTabViewModel InputMappingsTabViewModel(ArtemisDevice device, ObservableCollection<ArtemisLed> selectedLeds);
    }

    public interface ISidebarVmFactory : IVmFactory
    {
        SidebarViewModel SidebarViewModel(IScreen hostScreen);
        SidebarCategoryViewModel SidebarCategoryViewModel(ProfileCategory profileCategory);
        SidebarProfileConfigurationViewModel SidebarProfileConfigurationViewModel(ProfileConfiguration profileConfiguration);
    }

    public interface SurfaceVmFactory : IVmFactory
    {
        SurfaceDeviceViewModel SurfaceDeviceViewModel(ArtemisDevice device);
        ListDeviceViewModel ListDeviceViewModel(ArtemisDevice device);
    }
}