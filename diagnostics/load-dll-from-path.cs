using System;
using System.IO;
using System.Reflection;

public class CPHInline
{
    public bool Execute()
    {
        // Set this to the full path of iandouglas736.dll you want to test.
        string dllPath = args.ContainsKey("dllPath") ? args["dllPath"].ToString() : null;

        if (string.IsNullOrWhiteSpace(dllPath))
        {
            CPH.SendMessage("Set %dllPath% argument to the full path of iandouglas736.dll");
            return false;
        }

        if (!File.Exists(dllPath))
        {
            CPH.LogError($"File not found: {dllPath}");
            CPH.SendMessage($"File not found: {dllPath}");
            return false;
        }

        try
        {
            CPH.LogInfo($"Attempting Assembly.LoadFrom: {dllPath}");
            var asm = Assembly.LoadFrom(dllPath);
            CPH.LogInfo($"Loaded OK: {asm.FullName} from {asm.Location}");
        }
        catch (Exception ex)
        {
            CPH.LogError($"LoadFrom failed: {ex.GetType().Name}: {ex.Message}");
            CPH.LogError($"Details: {ex}");
        }

        try
        {
            CPH.LogInfo("Attempting Type.GetType for iandouglas736.Chat");
            var t = Type.GetType("iandouglas736.Chat, iandouglas736");
            CPH.LogInfo(t != null ? $"Type resolved: {t.AssemblyQualifiedName}" : "Type resolved as null");
        }
        catch (Exception ex)
        {
            CPH.LogError($"Type resolution failed: {ex.GetType().Name}: {ex.Message}");
        }

        return true;
    }
}
