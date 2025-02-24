using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Artemis.Core;
using Artemis.UI.Shared.Providers;
using Avalonia.Platform;

namespace Artemis.UI.Windows.Providers;

public class AutoRunProvider : IAutoRunProvider
{
    private readonly IAssetLoader _assetLoader;

    public AutoRunProvider(IAssetLoader assetLoader)
    {
        _assetLoader = assetLoader;
    }

    private async Task<bool> IsAutoRunTaskCreated()
    {
        Process schtasks = new()
        {
            StartInfo =
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                Arguments = "/TN \"Artemis 2 autorun\""
            }
        };

        schtasks.Start();
        await schtasks.WaitForExitAsync();
        return schtasks.ExitCode == 0;
    }

    private async Task CreateAutoRunTask(TimeSpan autoRunDelay)
    {
        await using Stream taskFile = _assetLoader.Open(new Uri("avares://Artemis.UI.Windows/Assets/autorun.xml"));

        XDocument document = await XDocument.LoadAsync(taskFile, LoadOptions.None, CancellationToken.None);
        XElement task = document.Descendants().First();

        task.Descendants().First(d => d.Name.LocalName == "RegistrationInfo").Descendants().First(d => d.Name.LocalName == "Date")
            .SetValue(DateTime.Now);
        task.Descendants().First(d => d.Name.LocalName == "RegistrationInfo").Descendants().First(d => d.Name.LocalName == "Author")
            .SetValue(WindowsIdentity.GetCurrent().Name);

        task.Descendants().First(d => d.Name.LocalName == "Triggers").Descendants().First(d => d.Name.LocalName == "LogonTrigger").Descendants().First(d => d.Name.LocalName == "Delay")
            .SetValue(autoRunDelay);

        task.Descendants().First(d => d.Name.LocalName == "Principals").Descendants().First(d => d.Name.LocalName == "Principal").Descendants().First(d => d.Name.LocalName == "UserId")
            .SetValue(WindowsIdentity.GetCurrent().User!.Value);

        task.Descendants().First(d => d.Name.LocalName == "Actions").Descendants().First(d => d.Name.LocalName == "Exec").Descendants().First(d => d.Name.LocalName == "WorkingDirectory")
            .SetValue(Constants.ApplicationFolder);
        task.Descendants().First(d => d.Name.LocalName == "Actions").Descendants().First(d => d.Name.LocalName == "Exec").Descendants().First(d => d.Name.LocalName == "Command")
            .SetValue("\"" + Constants.ExecutablePath + "\"");

        string xmlPath = Path.GetTempFileName();
        await using (Stream fileStream = new FileStream(xmlPath, FileMode.Create))
        {
            await document.SaveAsync(fileStream, SaveOptions.None, CancellationToken.None);
        }

        Process schtasks = new()
        {
            StartInfo =
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas",
                FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                Arguments = $"/Create /XML \"{xmlPath}\" /tn \"Artemis 2 autorun\" /F"
            }
        };

        schtasks.Start();
        await schtasks.WaitForExitAsync();

        File.Delete(xmlPath);
    }

    private async Task RemoveAutoRunTask()
    {
        Process schtasks = new()
        {
            StartInfo =
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true,
                Verb = "runas",
                FileName = Path.Combine(Environment.SystemDirectory, "schtasks.exe"),
                Arguments = "/Delete /TN \"Artemis 2 autorun\" /f"
            }
        };

        schtasks.Start();
        await schtasks.WaitForExitAsync();
    }

    /// <inheritdoc />
    public async Task EnableAutoRun(bool recreate, int autoRunDelay)
    {
        // if (Constants.BuildInfo.IsLocalBuild)
        // return;

        // Create or remove the task if necessary
        bool taskCreated = false;
        if (!recreate)
            taskCreated = await IsAutoRunTaskCreated();
        if (!taskCreated)
            await CreateAutoRunTask(TimeSpan.FromSeconds(autoRunDelay));
    }

    /// <inheritdoc />
    public async Task DisableAutoRun()
    {
        bool taskCreated = await IsAutoRunTaskCreated();
        if (taskCreated)
            await RemoveAutoRunTask();
    }
}