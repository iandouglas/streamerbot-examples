# Multi-Item Raffle Giveaway System

This is an idea that I started building in late 2022, had working in Python in July of 2023, and started rewriting in C# in August 2023. It was working okay but all giveaways were put on hold in October/November 2023 to take care of some everyday-life stuff.

The system needs a bit of an overhaul.

Basically the system is granting points for users watching the stream and other interactive things, and then users can use points to buy raffle tickets for specific raffle items that the streamer sets up. This way they only win the items they actually want, instead of that awkward moment of entering a giveaway for one of several items and maybe winning an item they didn't want.

The system works really well, but I want to move it to using more of Streamer.bot's internal LiteDB database. I built it to use my own LiteDB database, which worked fine, but made it nearly impossible for non-programmer streamers to edit/manipulate data without a UI of some sort. By moving some of the information to Streamer.bot's main internal LiteDB database using global and user persisted globals, the streamer can better manipulate and adjust things without having to know C# programming.

## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
