# Twitch Bits to play media files, managed via Google Sheets

### Brief description:

Build a Google Sheets spreadsheet in a very specific format, with bit values and media files or text to display.

### Why?

Because building a single Streamer.bot action and OBS source for EVERY media you want for bit redeems can quickly get out of hand.

This mechanism has exactly TWO Streamer.bot actions, and you can reuse OBS sources if you choose.

## Restrictions

Right now this will queue up one things at a time. If you REALLY want the chaos of multiple media files playing at the same time, you can remove a few sub-actions from the main command where it pauses and restarts the queue.

## How it works:

An action will run when Streamer.bot is alerted by Twitch that you have started streaming. This action will load your Google Sheet in CSV format in the background and prepare everything it needs.

The primary action is set up to run whenever any Twitch "cheer" is done in any amount. If it finds a corresponding match from the Google Sheet, that media will be loaded into the OBS scene/source you specify.

You can have separate audio and video files, as well as text inputs to display all with the same bit redeem value, allowing you to have a background video, a foreground audio, and maybe some overlaid text all at the same time.

## Google Sheets setup

I recommend you open this example Google Sheet and click on "File -> Make a Copy":

https://docs.google.com/spreadsheets/d/1St-YenWGiz7QBKt3MVVLkQochfsGBLTZ0B-Lc04Xq20/edit#gid=0

Alternatively, you can build your own Google Sheet from scratch, but MUST follow a very specific format of columns, and the column names are CASE-SENSITIVE and must be written exactly as follows:

- Column A must be called "Bits"
- Column B must be called "Filename"
- Column C must be called "Type"
- Column D must be called "OBSSource"
- Column E must be called "OBSScene"
- Column F must be called "Duration"

**IMPORTANT** you MUST set the document sharing to be visible by Streamer.bot:

1. Click the "Share" button
2. Under "General Access", set this to "Anyone with the link" and set that to "Viewer"

Next, copy the full address bad of your Google Sheets and use it below in the installation instructions.

### Field explanations:

- *Bits* -- this is the number of bits you want someone to redeem to play this media file. If you want to have multiple media files play at the same time, you can have multiple rows with the same bit value.
- *Filename* -- this is the full disk path of where your media files (movies, audio, etc) are located. You can enter this as "C:\path\to\filename.mp4" or "c:/path/to/filename.mp4", it doesn't matter. For overlaid text, this "Filename" field will actually contain the text you want to display, not a separate text file.
- *Type* -- set this to "txt" (lowercase) if you want Filename to be interpreted as the overlay text; you can leave this blank for other files
- *OBSScene* -- this is the OBS scene name where your "source" will be found in which your media file will load. The name is CASE-SENSITIVE and must match exactly what you have it called in OBS. If you have a scene called "Starting Soon" and you enter "starting soon" here, it will not work.
- *OBSSource* -- this is the OBS source name where your media file (or GDI text) will be loaded. This is case-sensitive, so if you have a source called "Media" and you enter "media" here, it will not work.
- *Duration* -- this is the number of seconds you want this media file to play. If you leave this blank, the code will attempt to determine the duration of the media file and play it for that length of time. If you enter a value here, it will override the duration of the media file. For overlay text, this is how many seconds you want to display the text on OBS. If you're going to overlay text on a video, set this value to a REALLY large value like 10000 seconds.

For the Duration field, if you have multiple rows with the same bit value, the code will determine the SMALLEST amount of duration of audio and video or text, and use that as the total time to show ALL of those elements.

For example, if you have a background video that is 60 seconds long, and an audio file which is only 15 seconds long, the code will determine that 15 seconds is the shorter length, and only show the video and play the audio for the same 15 seconds.

If you have a background video of 60 seconds, and and audio file of 15 seconds, and you also include a text overlay and set the text duration to 10 seconds, it becomes the shortest duration, so the video, audio, and text will all play for 10 seconds. If you set the text duration to 10000 seconds, the audio will have the shortest duration, so everything will play for only 15 seconds.


## Requirements:

1. A Google Sheets file that you can update and give viewing permissions
2. Knowledge of where your media files are
3. OBS scenes and sources for where you want to play the media files
4. A few DLL files (included in this repo) are required to be in your Streamer.bot's "dlls" folder.


## Installation

Check out the [import code](./import_code.txt) file, copy that to your clipboard, click on the "Import" button in Streamer.bot and paste it in the "Import String" field. Click on the Import button. This should import two actions:

1. Load CSV data from Google Sheets
  - there is a subaction that sets an argument called "GoogleSheetURL", set this to the FULL address bar URL of your Google Sheet.
  - this action loads your Google Sheet into Streamer.bot
  - if Streamer.bot is closed, the settings will be lost
  - there are two triggers for this action:
	- when you start streaming (Twitch Stream Online)
	- a manual reload of the data ("Misc test")

2. Bit redeems media and audio from CSV
  - this action checks all bit redeems for a match in the Google Sheet data and plays the files found, if any
  	- if no matches are found, nothing happens
  - you can manually test this with the "Misc test" trigger by double-clicking it and entering a "bits" value

Once you have these commands imported, you will need to put the following DLL files into your Streamer.bot "dlls" folder, and tell Streamer.bot to "compile" the code:

- [CsvHelper.dll](./dlls/CsvHelper.dll), found from [https://joshclose.github.io/CsvHelper/](https://joshclose.github.io/CsvHelper/)
	- this is used to read the CSV data from Google Sheets
- [Duration.Mine.Mp4.dll](./dlls/Duration.Mine.Mp4.dll), found from [https://github.com/sangeren/mp4Duration](https://github.com/sangeren/mp4Duration)
	- this is used to determine the duration of MP4 files
- [NLayer.dll](./dlls/NLayer.dll) and [NLayer.AudioSupport.dll](./dlls/NLayer.AudioSupport.dll), found from [https://github.com/naudio/NLayer](https://github.com/naudio/NLayer)
	- this is used to determine the duration of MP3 files
- [Microsoft.Bcl.HashCode.dll](./dlls/Microsoft.Bcl.HashCode.dll), found from [https://github.com/dotnet/corefx](https://github.com/dotnet/corefx)
	- this is used by CsvHelper to put CSV data into memory
- [netstandard.dll](./dlls/netstandard.dll), found from installing .NET 4.7.2, and used as general support for the other DLLs

Once you have these DLLs in your Streamer.bot "dlls" folder, you will need to tell Streamer.bot to "compile" the code.

Click on each of the two actions that were imported and do the following steps:

1. Double-click on the "Execute Code" sub-action. This will open a new code window. Don't change any code.
2. Click the "Find Refs" button.
3. Click the "Compile" button. If you see "Building out needed information..." and "Compiled successfully!" in the "Compiling Log" tab, you're all set. Click on the "Save & Compile" button to close that window.

If you see errors about "assembly references", then you will need to do a few more steps:

1. In the bottom of that window where you see the compiler errors, there is a tab called "References". Click on that.
2. Right-click in the panel where it lists some filenames ending in ".dll" and select "Add reference from file".
3. Navigate to your Streamer.bot folder, then into the "dlls" folder, and add each of the DLL files listed above.
4. Click on the "Compiling Log" tab to the left of the "References" tab, and click the "Compile" button again.

If you still get errors, check the Troubleshooting section below.

## Advanced feature, change the "queue"

I built this where everything happens on the "default" queue in Streamer.bot. If you want to use a different queue, then you'll need to create a new queue first:

1. Click on the "Action Queues" tab at the top of Streamer.bot
2. Click on the "Queues" tab
3. Right-click in an empty area of the lie of queues and click "Add", give it a name, and whether it should block other things happening, click OK

Then update the two actions that were imported by going back to the "Actions" tab at the top of Streamer.bot

1. Double-click on the action name
2. Pick your new queue from the drop-down list, click OK
3. (repeat for both commands)

## Advanced feature, "base path"

In my sample Google Sheet you'll see a row with a bit value of "-1" and a "Type" field called "base_path". This is an optional line that will set a base disk path for ALL media files in your spreadsheet if you don't want to type the same "C:\obs\assets" string over and over. Note that if you have media files on multiple disk drives, you should NOT use this feature.

## Advanced feature, remove the queue pausing

Okay, so you really, really, really want chaos and want all things to play all the time no matter what?

In the subaction for the main command ("Bit redeems media and audio from CSV"), remove all the other subactions EXCEPT for the Execute Code subaction.

## Future plans

If people want it badly enough, here are some ideas for future expansion:

- adding multiple sources, maybe pipe-delimited, like `source1|source2` and have the code randomly pick one, or analyze if one is already active, to use the next one (round-robin)
- support for other audio/video formats to determine their duration
- make it optional to only play one bit redeem at a time

## Troubleshooting

After you attempt to load the Google Sheet data, check the "Variables" in Streamer.bot and look at the "Non-Persisted Globals". If you do not see an item there called "ID736MediaCatalog" then your import did not work. Check your Google Sheets permissions. If you DO see data there, copy the value from the global variable and paste it over on JSONlint.com and click the "validate JSON" button to see if everything looks correct. If you see media files that aren't yours, you probably didn't set the correct "GoogleSheetsURL" argument.

Your Streamer.bot logs will write an info-level message if a file cannot be found. Double check spelling and case-sensitivity of your filenames

If you don't see log data about being unable to find the files, double-check your OBS scene names, and OBS source names.

If you continue to get compile errors, head over to [my Discord community](https://tig.fyi/discord) for help.
