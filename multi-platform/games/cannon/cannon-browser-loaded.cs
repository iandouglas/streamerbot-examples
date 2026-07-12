using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.SetGlobalVar("cannon_browser_loaded_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);

        return true;
    }
}
