# First, Second, Early Crew

For Twitch, you can set up a channel redeem for the first person who claims it, a second person, or an "early crew" of maybe the first 10 people.

This was built for [Amice_Ace](https://twitch.tv/amish_ace) who wanted to list these on their 'Starting Soon' screen.

This control will ONLY activate and allow those channel rewards to be redeemed while on that scene in OBS. If you change to a new scene the channel rewards will be disabled. If you change back to the scene, they will be re-enabled.

## What you need to set up ahead of time

### OBS
1. An OBS scene for your 'starting soon' page. Remember the exact name of this, it is case-sensitive.
2. Three fields of type `Text (GDI+)`. Remember their exact names, they are case-sensitive.

### In Streamer.bot

1. In Streamer.bot, you will need to click on Platforms -> Twitch -> Channel Point Rewards and set up three channel point redeems called 'first', 'second', and 'early crew'; these names are also case-sensitive.
2. For 'first', you'll set this as a "max 1 per stream"
3. For 'second', you'll set this as a "max 1 per stream"
4. For 'early crew', you'll set this as a "max 0 per stream" but "max 1 per User per Stream"

(for #4, you'll set the maximum count of users in your early gang later)

## Import the code from import_code.txt

Check out the [import code](./import_code.txt) file, copy that to your clipboard, click on the "Import" button in Streamer.bot and paste it in the "Import String" field. Click on the Import button.

Five actions will be imported. One called "first, second, early crew" will handle everything and needs arguments and options to be set by you.

The second action imported is called "first, second early crew -- RESET" will clear the values in memory and in OBS when you start streaming, stop streaming, or if you manually right-click on the "Misc Test" trigger, and select "test trigger". I recommend mapping this RESET action to a StreamDeck button if you have the hardware available so you can clear it manually if you don't want to try to find this in Streamer.bot to click around on a bunch of things.

The third and fourth actions, called "enable" and "disable" the "first/second/early redeems" will, respectively, enable or disable the three channel point redeems. Be sure to check the channel reward names are selected correctly in these actions' sub-actions.

The fourth is called "Check which scene we're on to enable/disable rewards" and will need you to double-click the "if" command in the subactions to write in the name of your "starting soon" scene. This is case-sensitive.

## Double-check the Action Triggers

The export will be set to channel redeems called `first!`, `second!` and `early crew`, so you'll need to set these to whatever you called your channel redeems in an earlier step.

To do this, right-click in the Triggers area, select Twitch -> Channel Reward -> Reward Redemption and pick your channel redeem names. You'll need to do this once per channel redeem so that you have three triggers.

## Adding your argument settings

For the "first, second, early crew" action, (not the RESET action), you will see a large number of settings:

1. `EarlyCrewSizeLimit` -- this is the maximum number of names you want to add to your early crew. These will be stacked, one per line, in your OBS text source, so be sure that it's big enough (or the font is small enough) to stack as many names as you intend to show on the OBS soure.
2. `EarlyCrewOBSScene` -- this is the case-sensitive OBS scene (like "Starting Soon") that you set up earlier.
3. `FirstOBSSource` -- this is the the case-sensitive OBS source for your text field for the "first" redeem username.
4. `SecondOBSSource` -- this is the case-sensitive OBS source for your text field for the "second" redeem username.
5. `EarlyCrewOBSSource` -- this is the case-sensitive OBS source for your text field for the "early crew" redeem username list.
6. `FirstChannelPointRedeemName` -- this is the case-sensitive Channel Redeem you made for the "first" user.
7. `SecondChannelPointRedeemName` -- this is the case-sensitive Channel Redeem you made for the "second" user.
8. `EarlyCrewChannelPointRedeemName` -- this is the case-sensitive Channel Redeem you made for the "early crew" users.
9. `FirstUsernamePrefix` -- This is a string to add before the username, like `First:` If any text is present in this field, it will add a space before the username, so it would appear as `First: iandouglas736` for example.
10. `SecondUsernamePrefix` -- This is a string to add before the username, like `Second:` If any text is present in this field, it will add a space before the username, so it would appear as `Second: iandouglas736` for example.
11. `EarlyCrewSeparator` -- this string will separate usernames in your early crew. You could set this as a carriage return as a backslash-N (`\n`, but no quotes around it) if you want a vertical stack, or a comma and space (`, `) to make the names appear comma delimited, for example. If no value is set here, the C# code will override this with a carriage return (`\n`) to make a vertical stack.
12. `EarlyCrewPrefix` -- This is a string to add before the early crew usernames, like `Early Crew:` If any text is present in this field, it will add a carriage return before any usernames so it might appear as such:
    ```text
    Early Crew:
    iandouglas736
    ```

The `EarlyCrewPrefix` will always be followed by a carriage return, no matter what you set as a string for `EarlyCrewSeparator`.

## Possible bug

There may be a bug that will happen if you use an "EarlyCrewSeparator" that contains a carriage return ("\n") but isn't JUST a carriage return. It might set your early crew to include the literal characters "\n" instead of treating it like a new line. Contact me on Discord if this happens to you.

## All done!

Once you set all of those arguments, you should be up and running.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://tig.fyi/discord) for help.

