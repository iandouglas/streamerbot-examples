# Google Sheets to list/control my channel announcements

I'd like to list out all of my channel announcements in a Google Sheets doc. In this doc, I'd like to list out the announcement, a frequency (ie, every 30 minutes), and a maximum posts per stream (ie, 3).

## Why?

Streamers often have things they want to post on a regular basis in chat. Some streamers will have contract agreements from sponsors, or want to regularly promote something. Sponsored chat messages, though, would probably have a limit, and sometimes you just don't want to annoy your viewers by a barrage of the same messages over and over and over.

## Considations

I'd probably want to add a control like "we need to see at least 5 chat messages from actual viewers (not other bots) to limit posting our announcements". Maybe this would be part of the CSV data as some things we'll want to post regardless of how active chat is.

I'd probably want to post this from my bot account, since I'd probably filter out my bot chats from chat archive like the Discord merge control, etc.

## Details

Arguments:
- GoogleSheetsURL, string
- post as Bot account, true/false
- doTwitch, true/false
- doYouTube, true/false

Ultimately, the doc will be pulled as CSV data, and stored in memory when a stream starts or when manually triggered by the streamer.

## Streamer.bot setup

Will need a one-minute resolution timer to run and to go through the saved announcements to determine if it's time to post a new announcement.

## technical thoughts

The data structure will need to include the text of the announcement, the frequency, and the maximum posts per stream, plus a "last posted" which will be cleared out when the stream begins but NOT if the streamer triggers a reload. (we don't want everything to replay if they've already hit the max-per-stream limit)


## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
