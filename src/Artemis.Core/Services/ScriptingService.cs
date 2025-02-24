﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Artemis.Core.ScriptingProviders;
using Ninject;
using Ninject.Parameters;

namespace Artemis.Core.Services;

internal class ScriptingService : IScriptingService
{
    private readonly List<GlobalScript> _globalScripts;
    private readonly IPluginManagementService _pluginManagementService;
    private readonly IProfileService _profileService;
    private readonly List<ScriptingProvider> _scriptingProviders;

    public ScriptingService(IPluginManagementService pluginManagementService, IProfileService profileService)
    {
        _pluginManagementService = pluginManagementService;
        _profileService = profileService;

        _pluginManagementService.PluginFeatureEnabled += PluginManagementServiceOnPluginFeatureToggled;
        _pluginManagementService.PluginFeatureDisabled += PluginManagementServiceOnPluginFeatureToggled;
        _scriptingProviders = _pluginManagementService.GetFeaturesOfType<ScriptingProvider>();
        _globalScripts = new List<GlobalScript>();

        ScriptingProviders = new ReadOnlyCollection<ScriptingProvider>(_scriptingProviders);
        GlobalScripts = new ReadOnlyCollection<GlobalScript>(_globalScripts);

        // No need to sub to Deactivated, scripts will deactivate themselves
        profileService.ProfileActivated += ProfileServiceOnProfileActivated;

        foreach (ProfileConfiguration profileConfiguration in _profileService.ProfileConfigurations)
        {
            if (profileConfiguration.Profile != null)
                InitializeProfileScripts(profileConfiguration.Profile);
        }
    }

    private GlobalScript CreateScriptInstance(ScriptConfiguration scriptConfiguration)
    {
        GlobalScript? script = null;
        try
        {
            if (scriptConfiguration.Script != null)
                throw new ArtemisCoreException("The provided script configuration already has an active script");

            ScriptingProvider? provider = _scriptingProviders.FirstOrDefault(p => p.Id == scriptConfiguration.ScriptingProviderId);
            if (provider == null)
                throw new ArtemisCoreException($"Can't create script instance as there is no matching scripting provider found for the script ({scriptConfiguration.ScriptingProviderId}).");

            script = (GlobalScript) provider.Plugin.Kernel!.Get(
                provider.GlobalScriptType,
                CreateScriptConstructorArgument(provider.GlobalScriptType, scriptConfiguration)
            );

            script.ScriptingProvider = provider;
            script.ScriptingService = this;
            scriptConfiguration.Script = script;
            provider.InternalScripts.Add(script);

            return script;
        }
        catch (Exception e)
        {
            script?.Dispose();
            throw new ArtemisCoreException("Failed to initialize global script", e);
        }
    }

    private ProfileScript CreateScriptInstance(ScriptConfiguration scriptConfiguration, Profile profile)
    {
        ProfileScript? script = null;
        try
        {
            if (scriptConfiguration.Script != null)
                throw new ArtemisCoreException("The provided script configuration already has an active script");

            ScriptingProvider? provider = _scriptingProviders.FirstOrDefault(p => p.Id == scriptConfiguration.ScriptingProviderId);
            if (provider == null)
                throw new ArtemisCoreException($"Can't create script instance as there is no matching scripting provider found for the script ({scriptConfiguration.ScriptingProviderId}).");

            script = (ProfileScript) provider.Plugin.Kernel!.Get(
                provider.ProfileScriptType,
                CreateScriptConstructorArgument(provider.ProfileScriptType, profile),
                CreateScriptConstructorArgument(provider.ProfileScriptType, scriptConfiguration)
            );

            script.ScriptingProvider = provider;
            scriptConfiguration.Script = script;
            provider.InternalScripts.Add(script);
            lock (profile)
            {
                profile.AddScript(script);
            }

            return script;
        }
        catch (Exception e)
        {
            // If something went wrong but the script was created, clean up as best we can
            if (script != null)
            {
                if (profile.Scripts.Contains(script))
                    profile.RemoveScript(script);
                else
                    script.Dispose();
            }

            throw new ArtemisCoreException("Failed to initialize profile script", e);
        }
    }

    private ConstructorArgument CreateScriptConstructorArgument(Type scriptType, object value)
    {
        // Limit to one constructor, there's no need to have more and it complicates things anyway
        ConstructorInfo[] constructors = scriptType.GetConstructors();
        if (constructors.Length != 1)
            throw new ArtemisCoreException("Scripts must have exactly one constructor");

        // Find the ScriptConfiguration parameter, it is required by the base constructor so its there for sure
        ParameterInfo configurationParameter = constructors.First().GetParameters().First(p => value.GetType().IsAssignableFrom(p.ParameterType));

        if (configurationParameter.Name == null)
            throw new ArtemisCoreException($"Couldn't find a valid constructor argument on {scriptType.Name} with type {value.GetType().Name}");
        return new ConstructorArgument(configurationParameter.Name, value);
    }

    private void InitializeProfileScripts(Profile profile)
    {
        // Initialize the scripts on the profile
        foreach (ScriptConfiguration scriptConfiguration in profile.ScriptConfigurations.Where(c => c.Script == null && _scriptingProviders.Any(p => p.Id == c.ScriptingProviderId)))
            CreateScriptInstance(scriptConfiguration, profile);
    }

    private void PluginManagementServiceOnPluginFeatureToggled(object? sender, PluginFeatureEventArgs e)
    {
        _scriptingProviders.Clear();
        _scriptingProviders.AddRange(_pluginManagementService.GetFeaturesOfType<ScriptingProvider>());

        foreach (ProfileConfiguration profileConfiguration in _profileService.ProfileConfigurations)
        {
            if (profileConfiguration.Profile != null)
                InitializeProfileScripts(profileConfiguration.Profile);
        }
    }

    private void ProfileServiceOnProfileActivated(object? sender, ProfileConfigurationEventArgs e)
    {
        if (e.ProfileConfiguration.Profile != null)
            InitializeProfileScripts(e.ProfileConfiguration.Profile);
    }

    /// <inheritdoc />
    public ReadOnlyCollection<ScriptingProvider> ScriptingProviders { get; }

    /// <inheritdoc />
    public ReadOnlyCollection<GlobalScript> GlobalScripts { get; }

    /// <inheritdoc />
    public ProfileScript AddScript(ScriptConfiguration scriptConfiguration, Profile profile)
    {
        profile.AddScriptConfiguration(scriptConfiguration);
        return CreateScriptInstance(scriptConfiguration, profile);
    }

    /// <inheritdoc />
    public void RemoveScript(ScriptConfiguration scriptConfiguration, Profile profile)
    {
        profile.RemoveScriptConfiguration(scriptConfiguration);
    }

    /// <inheritdoc />
    public GlobalScript AddScript(ScriptConfiguration scriptConfiguration)
    {
        throw new NotImplementedException("Global scripts are not yet implemented.");
    }

    /// <inheritdoc />
    public void RemoveScript(ScriptConfiguration scriptConfiguration)
    {
        throw new NotImplementedException("Global scripts are not yet implemented.");
    }
}