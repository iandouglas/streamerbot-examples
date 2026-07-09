using System;
using id736 = iandouglas736;

public class CPHInline
{
    public bool Execute()
    {
        // Do not clear the queue here. The browser reconnects every time the OBS
        // source becomes visible, and clearing the queue would discard shots that
        // were queued while the source was hidden. Just reset the setup flag so the
        // game re-sends its state to the freshly loaded browser.
        CPH.SetGlobalVar("cannon_firing", false, false);
        CPH.SetGlobalVar("cannon_firing_started", 0L, false);
        CPH.SetGlobalVar("cannon_setup_sent", false, false);
        CPH.SetGlobalVar("cannon_browser_loaded_at", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), false);

        return true;
    }
}
