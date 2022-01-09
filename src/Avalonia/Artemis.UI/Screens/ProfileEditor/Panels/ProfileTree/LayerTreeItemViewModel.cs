﻿using Artemis.Core;
using Artemis.UI.Ninject.Factories;
using Artemis.UI.Shared.Services.Interfaces;
using Artemis.UI.Shared.Services.ProfileEditor;

namespace Artemis.UI.Screens.ProfileEditor.ProfileTree
{
    public class LayerTreeItemViewModel : TreeItemViewModel
    {
        public LayerTreeItemViewModel(TreeItemViewModel? parent, Layer layer, IWindowService windowService, IProfileEditorService profileEditorService,
            IProfileEditorVmFactory profileEditorVmFactory) : base(parent, layer, windowService, profileEditorService, profileEditorVmFactory)
        {
            Layer = layer;
        }

        public Layer Layer { get; }
    }
}