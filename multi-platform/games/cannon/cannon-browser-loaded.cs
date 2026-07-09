using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        CPH.LogDebug("[cannon-loaded] Browser page loaded; clearing queue and resetting firing state.");

        CPH.SetGlobalVar("cannon_queue", "[]", false);
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.SetGlobalVar("cannon_browser_loaded_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);

        CPH.LogDebug("[cannon-loaded] Queue cleared and load timestamp set.");
        return true;
    }
}
