# Lurk / Unlurk with a stealth check dice roll

This emulates a 'roll for stealth' from tabletop gaming.

When a user types `!lurk` it will do a d20 disce roll and compare it against the streamer's d20 roll to determine if they successfully pass a stealth check. There are no consequences other than a funny chat message.

They can `!unlurk` which will add a response welcoming them back to the stream.

## set up two commands

You'll need to set up a `!lurk` and `!unlurk` command for incoming chat messages

## set up the Action

Add a new Action, and you will set two triggers, one for each of the `!lurk` abnd `!unlurk` commands you added.

Add a subaction to "Execute C# Code" and paste in the "lurk-unlurk.cs" code. Click the "Compile" button to make sure it compiles okay.

