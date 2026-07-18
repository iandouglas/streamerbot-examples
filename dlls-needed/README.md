:# iandouglas736 DLLs Needed

This folder contains the compiled helper DLL and its optional media-duration dependencies. Copy these files to your Streamer.bot PC and add them as Common References in Streamer.bot.

---

## Files in this folder

| File | Required? | Purpose |
|---|---|---|
| `iandouglas736.dll` | **Yes** | The main helper library. Provides `Chat`, `Groups`, `Points`, `PlatformConfig`, `Media`, `Data`, and `GoogleSheets` helpers. |
| `NLayer.dll` | MP3 duration | Used by `iandouglas736.Media` to read `.mp3` file durations. |
| `NLayer.NAudioSupport.dll` | NAudio MP3 | Supplemental library for mp3 files. |
| `Duration.Mine.Mp4.dll` | MP4 duration | Used by `iandouglas736.Media` to read `.mp4` file durations. |

If you do not play `.mp3` or `.mp4` files from Streamer.bot actions, you only need `iandouglas736.dll`.

---

## Where to put the DLLs

The DLLs can live either directly in your **Streamer.bot installation folder** or in a subfolder of your choice (many streamers use a `DLLs` folder). The important thing is that **all referenced DLLs live in the same folder** and that you reference them from that same location in Streamer.bot.

Common locations:

- `C:\Program Files\Streamer.bot\`
- `C:\Program Files\Streamer.bot\DLLs\`
- A different disk/path folder like `D:\Streamer.bot\DLLs\`

Put all of these files together in the same folder:

- `iandouglas736.dll`
- `NLayer.dll`
- `Duration.Mine.Mp4.dll`
- `NLayer.NAudioSupport.dll`

If you split the DLLs across different folders, you may see `FileNotFoundException` at runtime because Streamer.bot resolves assemblies relative to where the main reference is located.

---

## How to add the DLLs to Streamer.bot

1. Open **Streamer.bot**.
2. Go to **Settings → C# Compile Settings**.
3. Repeat the following steps for each DLL file:
  a. In the **Common References** area, right-click and choose **Add Reference from File**.
  b. Browse to the folder where you placed the DLLs. (protip, in the navigation window where it might look like "This PC > C: > Program Files", click in that address bar, it will give you the full proper disk path like "C:\Program Files\" which you can then copy to your clipboard )
  c. Select the DLL file
  d. Click **Open**.
8. Restart Streamer.bot

If you ever grab a fresh copy of the DLL files from this GitHub repository, you can replace the files in the same path. Streamer.bot will need to be fully closed to overwrite the DLL files on your disk though, as Streamer.bot may have them loaded in memory and you'll get an error from Windows trying to overwrite the files until Streamer.bot is fully shut down.

---

## Using the DLL library, iandouglas736.dll

Every `Execute C# Code` sub-action can use the full namespace:

```csharp
using iandouglas736;

Chat.SendMessage("Hello chat!");
```

Or a short alias that I prefer to use in case you have a different library that might implement a `Chat` functionality:

```csharp
using id736 = iandouglas736;

id736.Chat.SendMessage("Hello chat!");
```

---

## Source code

The source for `iandouglas736.dll` is open-source and lives in [`../DLL`](../DLL).

Build instructions are in [`../DLL/README.md`](../DLL/README.md).

The API reference is in [`../DLL/API.md`](../DLL/API.md) for all of the different things it can do for you.

---

## License

See the repository `LICENSE` file. Same license as the rest of this repository.
