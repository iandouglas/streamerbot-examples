# Channel point redeem or bit redeem to create a poll, only for VIP/mods

Allow users who the streamer flags as a VIP or mod to redeem channel points or bits to create a poll.

The redemption would put the user into a "poll-creator" group in Streamer.bot, and then they would be able to use a `!makepoll`` command to create a poll. Once a poll is successfully made, they are removed from that group.

## Thoughts

For simplicity, we probably want to use Streamer.bot to create a poll where each viewer can only vote one time.

Only one poll can be active at a time on Twitch, so we'd need to configure a cooldown when the command succeeds. If the cooldown is still active, just reply with a message saying "there's already a poll active, please try again later".

The channel point reward would need a prompt, and anything following the bit cheer would be the prompt. That prompt would have to follow very specific formatting to parse properly into a Twitch poll.

For example, "cheer500 Which game should I play next?|COD|Jedi Survivor|Minecraft|Tetris|Animal Crossing" would create a poll with the title "Which game should I play next?" and the options "COD", "Jedi Survivor", "Minecraft", "Tetris", and "Animal Crossing".

Also, the poll can only have 5 options ... if you goof up you stay in the group to try again.


## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
