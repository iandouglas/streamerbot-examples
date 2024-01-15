# Merge all platform chat to Discord

For streamers who multi-stream on multiple channels, it might be convenient for Streamer.bot to merge all chats into Discord for the streamer to see all chat happening in one place. This would also give the community an archive of chat history to look back on.

Ideally, this would be done in a Discord "thread", not all as separate messages in a single channel, as this could become very tiresome, and the streamer can then delete any single thread they no longer want to keep.

Needs:

- a Discord community
- a Discord channel webhook

in Streamer.bot:

- any chat command that does not trigger some other command should be copied to the Discord webhook, perhaps with an icon and username of who wrote the message
- would need to strip out Twitch emotes since those won't show up in Discord without other processing
    - this could be done, though

Downsides:

- if a streamer also multistreams to channels not yet implemented in Streamer.bot, eg Kick, Instagram, etc, this has less effect


## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
