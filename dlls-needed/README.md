:# iandouglas736 DLLs Needed

This folder contains the compiled helper DLL and its optional media-duration dependencies. Copy these files to your Streamer.bot PC and add them as Common References in Streamer.bot.

---

## Files in this folder

| File | Required? | Purpose |
|---|---|---|
| `iandouglas736.dll` | **Yes** | The main helper library. Provides `Chat`, `Groups`, `Points`, `PlatformConfig`, `Media`, `Data`, and `GoogleSheets` helpers. |
| `NLayer.dll` | Only for MP3 duration | Used by `iandouglas736.Media` to read `.mp3` file durations. |
| `NLayer.NAudioSupport.dll` | Only for NAudio MP3 | Optional. Only needed if you also use NAudio's `Mp3FileReader` with NLayer. `iandouglas736.Media` does not need it for basic MP3 duration detection. |
| `Duration.Mine.Mp4.dll` | Only for MP4 duration | Used by `iandouglas736.Media` to read `.mp4` file durations. |

If you do not play `.mp3` or `.mp4` files from Streamer.bot actions, you only need `iandouglas736.dll`.

---

## Where to put the DLLs

The DLLs can live either directly in your **Streamer.bot installation folder** or in a subfolder of your choice (many streamers use a `DLLs` folder). The important thing is that **all referenced DLLs live in the same folder** and that you reference them from that same location in Streamer.bot.

Common locations:

- `C:\Program Files\Streamer.bot\`
- `C:\Program Files\Streamer.bot\DLLs\`
- A portable folder like `D:\streamerbot\Streamer.bot\DLLs\`

Put all of these files together in the same folder:

- `iandouglas736.dll`
- `NLayer.dll` (only if you use `.mp3` duration)
- `Duration.Mine.Mp4.dll` (only if you use `.mp4` duration)
- `NLayer.NAudioSupport.dll` (only if you know you need it)

If you split the DLLs across different folders, you may see `FileNotFoundException` at runtime because Streamer.bot resolves assemblies relative to where the main reference is located.

---

## How to add the DLLs to Streamer.bot

1. Open **Streamer.bot**.
2. Go to **Settings → C# Compile Settings**.
3. In the **Common References** area, right-click and choose **Add Reference**.
4. Browse to the folder where you placed the DLLs.
5. Select `iandouglas736.dll`.
6. If you want media duration detection, also select:
   - `NLayer.dll` (for `.mp3`)
   - `Duration.Mine.Mp4.dll` (for `.mp4`)
   - `NLayer.NAudioSupport.dll` (only if you know you need NAudio support)
7. Click **OK**.
8. Restart Streamer.bot if the new references do not take effect immediately.

After that, every `Execute C# Code` sub-action can use the full namespace:

```csharp
using iandouglas736;

Chat.SendMessage("Hello chat!");
```

Or a short alias:

```csharp
using id736 = iandouglas736;

id736.Chat.SendMessage("Hello chat!");
```

---

## Updating the DLL

When a new version of `iandouglas736.dll` is released:

1. Copy the new `iandouglas736.dll` into the same folder where the old one lived (e.g. your Streamer.bot root or your `DLLs` subfolder), overwriting the old one.
2. In Streamer.bot, go to **Settings → C# Compile Settings**.
3. Remove the old `iandouglas736` reference from **Common References**.
4. Add the new `iandouglas736.dll` reference from that same folder.
5. Restart Streamer.bot.

---

## Source code

The source for `iandouglas736.dll` is open-source and lives in [`../DLL`](../DLL).

Build instructions are in [`../DLL/README.md`](../DLL/README.md).
The API reference is in [`../DLL/API.md`](../DLL/API.md).

---

## License

See the repository `LICENSE` file. Same license as the rest of this repository.
