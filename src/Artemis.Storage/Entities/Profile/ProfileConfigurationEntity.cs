﻿using System;
using Artemis.Storage.Entities.Profile.Nodes;

namespace Artemis.Storage.Entities.Profile;

public class ProfileConfigurationEntity
{
    public string Name { get; set; }
    public string MaterialIcon { get; set; }
    public string IconOriginalFileName { get; set; }
    public Guid FileIconId { get; set; }
    public int IconType { get; set; }
    public bool IconFill { get; set; }
    public int Order { get; set; }

    public bool IsSuspended { get; set; }
    public int ActivationBehaviour { get; set; }
    public NodeScriptEntity ActivationCondition { get; set; }

    public int HotkeyMode { get; set; }
    public ProfileConfigurationHotkeyEntity EnableHotkey { get; set; }
    public ProfileConfigurationHotkeyEntity DisableHotkey { get; set; }

    public string ModuleId { get; set; }

    public Guid ProfileCategoryId { get; set; }
    public Guid ProfileId { get; set; }
}