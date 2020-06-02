﻿using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Artemis.Core.Models.Profile;
using Artemis.Core.Services;
using Artemis.Core.Services.Interfaces;
using Artemis.UI.Properties;
using Artemis.UI.Services.Interfaces;
using Artemis.UI.Shared.Services.Interfaces;

namespace Artemis.UI.Screens.Module.ProfileEditor.Visualization.Tools
{
    public class SelectionToolViewModel : VisualizationToolViewModel
    {
        private readonly ILayerService _layerService;

        public SelectionToolViewModel(ProfileViewModel profileViewModel, IProfileEditorService profileEditorService, ILayerService layerService)
            : base(profileViewModel, profileEditorService)
        {
            _layerService = layerService;
            using (var stream = new MemoryStream(Resources.aero_crosshair))
            {
                Cursor = new Cursor(stream);
            }
        }

        public Rect DragRectangle { get; set; }

        public override void MouseUp(object sender, MouseButtonEventArgs e)
        {
            base.MouseUp(sender, e);

            var position = ProfileViewModel.PanZoomViewModel.GetRelativeMousePosition(sender, e);
            var selectedRect = new Rect(MouseDownStartPosition, position);

            // Get selected LEDs
            var selectedLeds = ProfileViewModel.GetLedsInRectangle(selectedRect);

            // Apply the selection to the selected layer layer
            if (ProfileEditorService.SelectedProfileElement is Layer layer)
            {
                // If shift is held down, add to the selection instead of replacing it
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    layer.AddLeds(selectedLeds.Except(layer.Leds));
                else
                {
                    layer.ClearLeds();
                    layer.AddLeds(selectedLeds);
                }

                ProfileEditorService.UpdateSelectedProfileElement();
            }
            // If no layer selected, apply it to a new layer in the selected folder
            else if (ProfileEditorService.SelectedProfileElement is Folder folder)
            {
                var newLayer = _layerService.CreateLayer(folder.Profile, folder, "New layer");
                newLayer.AddLeds(selectedLeds);
                ProfileEditorService.ChangeSelectedProfileElement(newLayer);
                ProfileEditorService.UpdateSelectedProfileElement();
                ProfileEditorService.UpdateSelectedProfile();
            }
            // If no folder selected, apply it to a new layer in the root folder
            else
            {
                var rootFolder = ProfileEditorService.SelectedProfile.GetRootFolder();
                var newLayer = _layerService.CreateLayer(rootFolder.Profile, rootFolder, "New layer");
                newLayer.AddLeds(selectedLeds);
                ProfileEditorService.ChangeSelectedProfileElement(newLayer);
                ProfileEditorService.UpdateSelectedProfileElement();
                ProfileEditorService.UpdateSelectedProfile();
            }
        }

        public override void MouseMove(object sender, MouseEventArgs e)
        {
            base.MouseMove(sender, e);
            if (!IsMouseDown)
            {
                DragRectangle = new Rect(-1, -1, 0, 0);
                return;
            }

            var position = ProfileViewModel.PanZoomViewModel.GetRelativeMousePosition(sender, e);
            var selectedRect = new Rect(MouseDownStartPosition, position);
            var selectedLeds = ProfileViewModel.GetLedsInRectangle(selectedRect);

            // Unless shift is held down, clear the current selection
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
                ProfileViewModel.SelectedLeds.Clear();
            ProfileViewModel.SelectedLeds.AddRange(selectedLeds.Except(ProfileViewModel.SelectedLeds));

            DragRectangle = selectedRect;
        }
    }
}