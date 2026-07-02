# "I have the high ground!"

Inspired by an idea by [Amish Ace on Twitch](https://twitch.tv/amish_ace), and the Star Wars movie, "Star Wars: Episode III – Revenge of the Sith" with the fight between Obi Wan Kenobi and Anakin Skywalker.

Your chat can use bits to play or channel points, whichever you want to enable.

There are a series of 5 actions, 2 commands, and potentially a channel reward.

## The 'highground' bits/channel actions

These actions are triggered each from an amount of bits (50 bits), or by a channel reward (set to 2500 channel points) that you can modify within Streamer.bot.

Both of these actions set 4 arguments:

- `videoFileWin` and `videoFileLose` -- double click on these to set a disk path of a video clip you'd like to play.
- `OBSScene` and `OBSSource` -- double click these to change the OBS scene and source where you'd like to play the win/lose videos.

Both of these actions the run an action called `highground logic`, described next.

## highground logic

The logic in this C# code checks which username spent Twitch bits or redeemed a channel reward.

The very first time this runs, it will set a memory variable called `highGroundCooldownSeconds` to a value of 120 seconds on line 26 of the C# code. Once this is set in memory you can modify this directly through the 'Global Variables' panel.

If someone claims the 'high ground', it will play the high ground 'win' video. If someone attempts to gain the high ground before 120 seconds is up, they will see the "lose" video. There are chat messages for each of these on line 65 for the win and line 42 for the someone who doesn't win.

### Run some C# code

Double-click on the "Execute Code" sub-action, then click on the 'Find Refs' button.

You will need to copy the DLL files from this subfolder to your Streamer.bot installation folder.

Next, you'll need to click on the 'References' tab and make sure the following files are added. Some of these will be found in your Windows\Microsoft.NET\Frameworks64\v4.0.30319\ folder (the v4.0.30319 may be different), and the others you'll navigate to your Streamer.bot installation folder where you made the 'dlls' folder and choose each of the 3 files:
- from your Windows\Microsoft.NET\Frameworks64\<version> path:
    - mscorlib.dll
    - System.dll
    - netstandard.dll
- from your Streamer.bot installation folder:
    - Newtonsoft.Json.dll (this file will already by in your Streamer.bot folder)
    - Duration.Mine.Mp4.dll
    - NLayer.dll
    - NLayer.NAudioSupport.dll

If any of these files are already listed in the 'References' tab, you can skip them. Right-click anywhere around the files listed and pick 'Add reference from file'. This should default to the Windows\Microsoft.NET\Frameworks64\<version> path on your system.

Make sure all 7 of these DLL files are in the list.

Next, click on the 'Compiling Log' tab, then click the "Compile" button at the bottom of the code window, and then the "Save and Compile" button to close the window. If the "Compile" button gives you an errors, try clicking the "Find Refs" button again, then the "Compile" button again. If you still have trouble, come ask in my [Discord community](https://736.fyi/discord) for free help.

You can right-click on the "Twitch > Ads" trigger and pick the "Test Trigger" at the top to watch your Twitch chat to see what will happen.

## !highground command

This chat command will allow your Twitch chat to see which other user currently holds the high ground, and how long until the 120 second timer is over.

## !hgleaders

This is a leaderboard showing the top usernames who have held the high ground, sorted by count and then alphabetically in case of a tie. If a user already has the high ground, their count will not increase, they will simply maintain the high ground.

## Support

If you need help, join [my Discord community](https://736.fyi/discord) and I'll provide free support.

