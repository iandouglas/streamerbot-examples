using System;
using System.Reflection;
using System.Linq;

public class CPHInline
{
    public bool Execute()
    {
        var domain = AppDomain.CurrentDomain;

        CPH.LogInfo("=== AppDomain probe info ===");
        CPH.LogInfo($"BaseDirectory: {domain.BaseDirectory}");
        CPH.LogInfo($"PrivateBinPath: {domain.SetupInformation.PrivateBinPath ?? "(null)"}");
        CPH.LogInfo($"ShadowCopyFiles: {domain.SetupInformation.ShadowCopyFiles}");
        CPH.LogInfo($"DisallowApplicationBaseProbing: {domain.SetupInformation.DisallowApplicationBaseProbing}");

        CPH.LogInfo("=== Loaded assemblies (name | version | location) ===");
        foreach (var asm in domain.GetAssemblies().OrderBy(a => a.FullName))
        {
            string loc = "(dynamic / no location)";
            try { loc = asm.Location; } catch { }
            CPH.LogInfo($"{asm.GetName().Name} | {asm.GetName().Version} | {loc}");
        }

        CPH.SendMessage("Probe info logged. Check Streamer.bot logs.");
        return true;
    }
}
