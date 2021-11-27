﻿using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Shared;
using Artemis.UI.Shared.Services.Builders;
using Artemis.UI.Shared.Services.Interfaces;
using ReactiveUI;
using RGB.NET.Core;

namespace Artemis.UI.Screens.Device
{
    public class DeviceDetectInputViewModel : ContentDialogViewModelBase
    {
        private readonly IInputService _inputService;
        private readonly INotificationService _notificationService;
        private readonly IRgbService _rgbService;
        private readonly ListLedGroup _ledGroup;

        public DeviceDetectInputViewModel(ArtemisDevice device, IInputService inputService, INotificationService notificationService, IRgbService rgbService)
        {
            _inputService = inputService;
            _notificationService = notificationService;
            _rgbService = rgbService;

            Device = device;

            // Create a LED group way at the top
            _ledGroup = new ListLedGroup(_rgbService.Surface, Device.Leds.Select(l => l.RgbLed))
            {
                Brush = new SolidColorBrush(new Color(255, 255, 0)),
                ZIndex = 999
            };

            this.WhenActivated(disposables =>
            {
                Observable.FromEventPattern(x => _inputService.DeviceIdentified += x, x => _inputService.DeviceIdentified -= x)
                    .Subscribe(_ => InputServiceOnDeviceIdentified())
                    .DisposeWith(disposables);

                Disposable.Create(() => _ledGroup.Detach()).DisposeWith(disposables);
            });
        }

        public ArtemisDevice Device { get; }
        public bool IsMouse => Device.RgbDevice.DeviceInfo.DeviceType == RGBDeviceType.Mouse;

        public bool MadeChanges { get; set; }

        private void InputServiceOnDeviceIdentified()
        {
            _notificationService.CreateNotification()
                .WithMessage($"{Device.RgbDevice.DeviceInfo.DeviceName} identified 😁")
                .WithSeverity(NotificationSeverity.Success)
                .Show();
        }
    }
}