# streamerbot-giveaway

This DLL module provides the ability for streamers to use Streamer.bot as a single-item or multi-item raffle giveaway system. Viewers gain points while watching your stream and spend those points to enter specific smaller raffles within your giveaway so they only win the prizes they actually want.

## What problem does this giveaway system solve?

Other online giveaway systems such as StreamElements or Lachh Tools do not allow you to give away multiple items where your viewers get to pick the item they win. This system helps to avoid disappointment where your viewers want to enter a giveaway, but maybe aren't excited about a particular prize and silently hope they don't win, don't respond to being drawn as a winner, leading to an awkward redraw scenario.

For example, I was on a live stream where the streamer had a points system, and allowed up to 1000 points to be accrued to enter a raffle giveaway for a prize item. One user had the necessary points but vocally said in chat they weren't excited about the prize, but then entered the giveaway, and that user was drawn as the winner. Next came the very awkward moment of the streamer trying to communicate with the user, live on stream, about whether they even wanted the prize or not, why did they enter if they didn't want it, should the streamer re-roll, etc.. It caused a lot of awkwardness on the stream.


## How does it all work?

The raffle system is split into two kinds of giveaways: short-term and long-term.

For short-term giveaways, you can create a single Streamer.bot action which launches a giveaway right away, and determines in advance the item you're giving away and number of winners you will draw. You cannot adjust the item or quantity of winners once you start the giveaway. After you select your winner(s), you manually close the giveaway.

Long-term giveaways allow you to have more than one prize and typically run over several streams, though you could specify a start/end time to happen within a single stream if you want. As viewers watch your stream, they accrue a virtual currency called "points". Each item you link to your giveaway will indicate the maximum number of points that an individual viewer can apply to that raffle. If a viewer has no interest in a particular item, they do not have to put points into the system.

## Documentation

Documentation and repository of exported commands will be coming soon.

## Customization of the Giveaway System

I am taking commissions on altering the giveaway system for individual streamers. You will get your own 'branch' on a private GitHub repository where I will generate a DLL specific to your stream. Contact me on Discord for more information.

Common customizations include things like changing the name of "points" to another term like "bits" or "coins" or changing the messages presented in the stream chat. Another common feature request that I might build into the 'core' system here is the ability to customize giveaway options/settings on a per-giveaway basis instead of only at a global level.

## Requirements / Installation

The DLL will require Streamer.bot 0.2.x. It utilizes new features found in 0.2.x and will not work with 0.1.x versions of Streamer.bot.

1. Download the latest DLL file from the release page
2. Within your Streamer.bot installation folder, you should find a folder called "dlls". Move the downloaded DLL file into that "dlls" folder
3. Restart Streamer.bot
4. Create your actions, or import any of the exported actions from the "Streamer.bot exports" folder in this repository.


## Support

Support can be done through my [Discord community](https://tig.fyi/discord), or by dropping by my own [live stream](https://twitch.tv/iandouglas736) to ask questions.

## Contributing Ideas

I absolute LOVE feedback and new ideas. Please please please -- NEVER hesitate to offer feedback or ideas. Use the "issues" area of GitHub to post any new ideas or bugs you find, or changes you'd like to see. I always welcome feedback on the things I create. However, please check if people have already submitted the idea and simply upvote their idea and add your own comments to an existing idea, instead of making a whole new post.


## F.A.Q.

**What kind of information is tracked about the viewers? I'm concerned about privacy.**

As soon as a viewer sends a chat message to your channel, the system will store their username, the platform ID (ie a unique Twitch ID value), and the date/time the record was created and last updated. A field is present to track the user's accumulated points, and optional fields to influence a bias score for winning, or to flag the user as a 'bot'.

**You said the system chose winners randomly. How does this "bias" score work?**

The system will still draw a winner purely randomly from the pool of entries. I was asked by several streamers when building this system to include a 'bias' score as a float value to influence the number of raffle tickets for a given user. Effectively, your number of raffle tickets is multiplied by this score (which defaults to 1.0, or 100%) when a prize is drawn. These streamers felt it was a great engagement tool to do things like offer subscribers or VIPs an increased chance to win by setting their score to 1.25, indicating a 25% increased chance to win, so someone who spends 100 points on raffle tickets would have 125 tickets as the raffle item was being drawn. And yes, this could be used to lower someone's chance of winning if the score is set less than 1.0. For example, a user who spends 100 points on raffle tickets who has a bias score of 0.5 would effectively only have 50 raffle entries as the item is being drawn, or a 50% reduced chance of winning. Ask your streamer to be transparent about this.

**What happens to accounts flagged as bots?**

My giveaway system will allow a streamer to flag an account as a bot, which sets their bias score to 0 and immediately stop processing any commands entered about the points system or entering any raffles. The system will preserve any previous bias score, and reset the bias score later if the streamer unsets the account as a bot. In short, it gives an account an absolute 0 chance of winning a raffle item.

**Will this work for YouTube streamers?**

Not yet. I'm primarily building this for Twitch, but will expand to YouTube in 2024. Streamers will have a giveaway option for each platform to send messages

**How does this differ from a system such as StreamElements?**

While I have no scientific proof, I believe that StreamElements has some amount of favoritism or bias built into the software of which they are not transparent to their users. I have entered StreamElements giveaways on several Twitch streams, and literally won something on a Sunday, Monday, Tuesday and two prizes on Wednesday, all in the same week, all from different streamers, and then never win anything at all for 4-5 months. Then I end up on a winning streak again. My giveaway system is purely random, and your chances of winning is purely based on the points entered for the raffle.

**I want to use the short-term giveaway but want more flexibility in how many winners that I draw. How do I do that?**

When you create the giveaway, specify a higher number of winners than you expect to actually draw. Eg, if you think you want to draw 2 or 3 winners, built the giveaway for 10 winners. Simply close the giveaway afterward.

**What the primary differences between a short-term and long-term giveaway**

* A short-term giveaway is meant to be an in-the-moment giveaway on the livestream, with a single item in mind. The streamer determines how many winners there will be ahead of time. Each user is given a single entry into the giveaway.
* A long-term giveaway can be scheduled for any duration of time in the future, and can have multiple items. Each item has a points "cap" per user so one user with a lot of points cannot overwhelm the system to guarantee a win. Viewers spend their points to enter a particular item raffle, and then are only drawn for the items where they have reserved points.

**What happens if something goes wrong, how do I get the right kind of information to you?**

First, be sure you're on the latest version of the DLL. Your primary source of information will be the log files for this giveaway system and the Streamer.bot log files. Both of these can be found in the "logs" folder where you installed Streamer.bot. Alternatively, for problems that you can replicate every time: Check the releases page for a "debug" version of the DLL and install that by overwriting your original DLL file. Run your commands again. This will export a LOT of log data about what's in your giveaway database. You'll also need to send me an export of your Streamer.bot commands and actions so I can help debug the problem. Replace the debug DLL with the non-debug version so you don't overwhelm your system writing log data. Compress your logs into a single .ZIP file, and contact me on Discord for how to send me your log data securely. Do NOT post your log data into Discord.

**Will you ever open-source the code to create this DLL?**

Not right away, no. I use this as a side business to generate income.

## Please check the LICENSE document for restrictions on use

Specifically, I do not allow derivatives of this project or the DLL files. You may not distribute this DLL to others, please just direct people back here to get their own copy if you like the project. If you want to include my DLL and documentation in another product/project, please contact me AHEAD OF TIME about partnership to best support you, whether or not you intend to make money off of my work.
