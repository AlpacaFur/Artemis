﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using Artemis.Core;
using Artemis.Core.Services;
using Artemis.UI.Shared.Services;
using Artemis.UI.Windows.Utilities;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Ninject;

namespace Artemis.UI.Windows;

public class ApplicationStateManager
{
    private readonly IWindowService _windowService;

    // ReSharper disable once NotAccessedField.Local - Kept in scope to ensure it does not get released
    private Mutex? _artemisMutex;

    public ApplicationStateManager(IKernel kernel, string[] startupArguments)
    {
        _windowService = kernel.Get<IWindowService>();
        StartupArguments = startupArguments;
        IsElevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        Core.Utilities.ShutdownRequested += UtilitiesOnShutdownRequested;
        Core.Utilities.RestartRequested += UtilitiesOnRestartRequested;

        // On Windows shutdown dispose the kernel just so device providers get a chance to clean up
        if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledApplicationLifetime)
            controlledApplicationLifetime.Exit += (_, _) =>
            {
                RunForcedShutdownIfEnabled();

                // Dispose plugins before disposing the kernel because plugins might access services during dispose
                kernel.Get<IPluginManagementService>().Dispose();
                kernel.Dispose();
            };

        // Inform the Core about elevation status
        kernel.Get<ICoreService>().IsElevated = IsElevated;
    }

    public string[] StartupArguments { get; }
    public bool IsElevated { get; }

    public bool FocusExistingInstance()
    {
        _artemisMutex = new Mutex(true, "Artemis-3c24b502-64e6-4587-84bf-9072970e535f", out bool createdNew);
        if (createdNew)
            return false;

        return RemoteFocus();
    }

    public void DisplayException(Exception e)
    {
        try
        {
            _windowService.ShowExceptionDialog("An unhandled exception occured", e);
        }
        catch
        {
            // ignored, we tried
        }
    }

    private bool RemoteFocus()
    {
        // At this point we cannot read the database yet to retrieve the web server port.
        // Instead use the method external applications should use as well.
        if (!File.Exists(Path.Combine(Constants.DataFolder, "webserver.txt")))
        {
            KillOtherInstances();
            return false;
        }

        string url = File.ReadAllText(Path.Combine(Constants.DataFolder, "webserver.txt"));
        using HttpClient client = new();
        try
        {
            CancellationTokenSource cts = new();
            cts.CancelAfter(2000);

            HttpResponseMessage httpResponseMessage = client.Send(new HttpRequestMessage(HttpMethod.Post, url + "remote/bring-to-foreground"), cts.Token);
            httpResponseMessage.EnsureSuccessStatusCode();
            return true;
        }
        catch (Exception)
        {
            KillOtherInstances();
            return false;
        }
    }

    private void KillOtherInstances()
    {
        // Kill everything else heh
        List<Process> processes = Process.GetProcessesByName("Artemis.UI.Windows").Where(p => p.Id != Process.GetCurrentProcess().Id).ToList();
        foreach (Process process in processes)
        {
            try
            {
                process.Kill(true);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }

    private void UtilitiesOnRestartRequested(object? sender, RestartEventArgs e)
    {
        List<string> argsList = new();
        argsList.AddRange(StartupArguments);
        if (e.ExtraArgs != null)
            argsList.AddRange(e.ExtraArgs.Except(argsList));
        string args = argsList.Any() ? "-ArgumentList " + string.Join(',', argsList) : "";
        string command =
            $"-Command \"& {{Start-Sleep -Milliseconds {(int) e.Delay.TotalMilliseconds}; " +
            "(Get-Process 'Artemis.UI.Windows').kill(); " +
            $"Start-Process -FilePath '{Constants.ExecutablePath}' -WorkingDirectory '{Constants.ApplicationFolder}' {args}}}\"";
        // Elevated always runs with RunAs
        if (e.Elevate)
        {
            ProcessStartInfo info = new()
            {
                Arguments = command.Replace("}\"", " -Verb RunAs}\""),
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "PowerShell.exe"
            };
            Process.Start(info);
        }
        // Non-elevated runs regularly if currently not elevated
        else if (!IsElevated)
        {
            ProcessStartInfo info = new()
            {
                Arguments = command,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                FileName = "PowerShell.exe"
            };
            Process.Start(info);
        }
        // Non-elevated runs via a utility method is currently elevated (de-elevating is hacky)
        else
        {
            string powerShell = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe");
            ProcessUtilities.RunAsDesktopUser(powerShell, command, true);
        }

        // Lets try a graceful shutdown, PowerShell will kill if needed
        if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledApplicationLifetime)
            Dispatcher.UIThread.Post(() => controlledApplicationLifetime.Shutdown());
    }

    private void UtilitiesOnShutdownRequested(object? sender, EventArgs e)
    {
        // Use PowerShell to kill the process after 8 sec just in case
        RunForcedShutdownIfEnabled();

        if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledApplicationLifetime)
            Dispatcher.UIThread.Post(() => controlledApplicationLifetime.Shutdown());
    }

    private void RunForcedShutdownIfEnabled()
    {
        if (StartupArguments.Contains("--disable-forced-shutdown"))
            return;

        ProcessStartInfo info = new()
        {
            Arguments = "-Command \"& {Start-Sleep -s 8; (Get-Process -Id " + Process.GetCurrentProcess().Id + ").kill()}",
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true,
            FileName = "PowerShell.exe"
        };
        Process.Start(info);
    }
}