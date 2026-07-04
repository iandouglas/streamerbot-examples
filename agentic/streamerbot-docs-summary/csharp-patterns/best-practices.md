# Streamer.bot C# Best Practices

## Priorities

1. Follow Streamer.bot's documented `CPH` API shape first.
2. Keep inline actions small, deterministic, and easy to rerun.
3. Use `TryGetArg<T>()` for argument access whenever possible.
4. Use globals only for deliberate shared state.
5. Use action chaining for orchestration and C# for logic-heavy steps.

## Recommended coding style inside `CPHInline`

- Validate every external input from args, globals, chat, or remote APIs.
- Favor early returns when a required argument is missing.
- Keep `Execute()` short and move logic into helper methods when the action grows.
- Log state transitions and failure paths with `CPH.LogInfo`, `CPH.LogWarn`, or `CPH.LogError`.
- Use `Init()` only for reusable setup that actually benefits from one-time compilation-time initialization.
- Use `Dispose()` to release any long-lived disposable resources.

## Live interactive control guidance

- Treat triggers as ingestion, variables as state, and actions as orchestration units.
- Keep chat-driven actions idempotent where practical so retries do not corrupt state.
- Prefer named arguments into child actions for clean action-to-action contracts.
- Separate viewer-facing effects from moderator/admin safety checks.
- For overlays or remote controls, favor WebSocket or HTTP entrypoints that simply enqueue named actions with validated args.

## Practical patterns

- Counter/state machine: use persisted globals with default fallbacks.
- Reusable utility action: encapsulate logic in one C# code action and call it through `RunAction` with args.
- Remote control: expose a minimal HTTP or WebSocket command surface that maps to approved action names/IDs.
- Platform branching: read `eventSource`/`__source` with the documented enum types instead of guessing from strings.

## Safe inline action template

```cs
using System;

public class CPHInline
{
    /// <summary>
    /// Executes a Streamer.bot inline action by validating inputs, updating shared state, and delegating follow-up work.
    /// </summary>
    /// <returns>
    /// True to continue downstream sub-actions; false to stop execution when required input is missing.
    /// </returns>
    public bool Execute()
    {
        if (!CPH.TryGetArg("user", out string user) || string.IsNullOrWhiteSpace(user))
            return false;

        int count = CPH.GetGlobalVar<int?>("interactionCount", true) ?? 0;
        count++;
        CPH.SetGlobalVar("interactionCount", count, true);

        CPH.LogInfo($"Interaction from {user}; count={count}");
        CPH.RunAction("Post Interaction Overlay", false);
        return true;
    }
}
```

## Helper-method pattern

```cs
using System;

public class CPHInline
{
    /// <summary>
    /// Executes the action after reading a validated command argument from Streamer.bot's argument stack.
    /// </summary>
    /// <returns>
    /// True when the action handled the request successfully; otherwise false.
    /// </returns>
    public bool Execute()
    {
        if (!CPH.TryGetArg("rawInput", out string rawInput))
            return false;

        return HandleInput(rawInput);
    }

    /// <summary>
    /// Handles a validated user input string and emits a log entry for later troubleshooting.
    /// </summary>
    /// <param name="rawInput">Non-null command or chat input provided by the current action context.</param>
    /// <returns>
    /// True when the input is accepted; otherwise false to halt downstream execution.
    /// </returns>
    public bool HandleInput(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return false;

        CPH.LogInfo($"Handling input: {rawInput}");
        return true;
    }
}
```
