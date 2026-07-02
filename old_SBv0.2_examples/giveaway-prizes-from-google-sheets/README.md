# Giveaway prizes from Google Sheets

Inspired by [DKlarations](https://twitch.tv/DKlarations)

Track my giveaway items (game codes, shirts, pins, stickers, etc) in a Google Sheets document and have Streamer.bot randomly choose an item from the spreadsheet to give away to a user in chat.

The mechanism will reduce the `!enter` chat spam (or other keyword the viewers might otherwise type in chat), and allow users to remain as a passive viewer in chat.

It will be the streamer's responsibility to move the giveaway information from one worksehet in the Google Sheets document to another, to prevent the same item from being given away twice. (unless you DO want this functionality)


## Installation

You can copy the text from [import_code.txt](import_code.txt) and paste it into your Streamer.bot's `import` command.

The code is provided in import_csv.cs and pick_winner.cs for your reference.

## Google Sheets format

Make a copy of this Google Sheet:
* https://docs.google.com/spreadsheets/d/1P8UJj6tGYRxwFp_R3bkQ8spDkPPy-nCKabDVcUyfRu8/edit#gid=0

The field names must remain as-is, if you change them the code will break.

## How It Works

The first command, "load giveaway csv data from Google Sheets", will load the data from the Google Sheets document into memory when Twitch tells Streamer.bot that your stream is online, or you can manually run this trigger yourself to reload the data at any time. When Streamer.bot is closed, this information is wiped.

The second command, "pick a winner from the giveaway csv data", will randomly choose a "type" of gift from the "Type" column, and then randomly pick an item from that type. It will then choose a random user who is actively present in chat.

THIS DOES NOT REMOVE THE ITEM FROM THE GOOGLE SHEETS DOCUMENT or edit the document in any way. It will be the streamer's responsibility (or a moderator if you give them access to the Google document) to move the row from the "items" sheet to the "winners" sheet.

Once this is moved, the streamer must reload the CSV data or that prize may get chosen again. If it's a recurring item such as your own merchandise, it's your choice whether the item needs to be "removed" from the "items" sheet in the spreadsheet or not.

There are several arguments to set in the code, listed below.

## A note about randomness

For transparency, the code is using C#'s "Random()" random number generator. This gets "seeded" with a value based on the number of system "ticks" plus a new univerally-unique identifier (UUID) generated for a one-time use. This is done to ensure that the random number generator is less "predictable".

This randomness is done between each random step of choosing a gift type, choosing a gift of that type, and choosing a user. This should be sufficient randomness to ensure the least predictable outcome.

## Arguments to Set

In the first command, "load giveaway csv data from Google Sheets", only a single argument is needed, called `GoogleSheetURL` which you will double-click to edit and paste in the URL of your Google Sheets document.

Be sure that your Google Sheets document is set to "Anyone with the link can view". Click on "Share", and under "General Access", click "Change to anyone with the link" and make sure the permission is set to "Viewer".

In the second command, "pick giveaway recipient", there are several arguments to set:

1. Add 1000000 random twitch users, present only 
   This will use Streamer.bot's awareness of the "Viewers" tab to pick a random user to be the recipient of the giveaway. Set this value to a really high number as the maximum number of users to choose from.

2. `excludeUsersFromGroup` 
   I recommend setting this to "Bots" (case-sensitive) and marking known bot accounts to that group. You can set users by right-clicking their username in the "Viewers" tab and choosing "Add to Group". This will prevent bots from being chosen as the winner.

3. `showInfoFieldInChat` 
   If set to "True", the bot will include the "Info" field in the chat message. This is useful if you want to include a link to a website or other information about the prize. This content will come from the "Info" column in the Google Sheets document.

4. `hideInfoForDigitalItem` 
   If set to "True", the bot will not include the "Info" field in the chat message if the "Type" column is set to "Digital". This is useful if you want the "Info" field in the spreadsheet to include a single-use game redemption code that you don't want everyone in chat to see.

5. `discordWebhookURL` 
   If you have a Discord server, you can create a webhook for a channel to copy the chat message for who the recipient was and what their gift is. This is useful if you want to have a record of who received an item, or other automation that you might want to set up.

## All done!

Once you set all of those arguments, you should be up and running.


## Future Ideas:

- for digital items, it would be nice to have Streamer.bot automatically send a whisper to the winner with the game code and where to redeem, or to remind them to submit their mailing address for physical items
- it might be nice to have a Google Form to fill out with contact info / mailing address for physical items and include that in the chat/discord message. (this would need to be added as another argument to the code)
- it might be nice to track a "winner" list to check over time to reduce someone's chance of winning another giveaway in a short amount of time.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://tig.fyi/discord) for help.

