# Improved !followage command

Let's build a cumulative `!followage` command that will track if you've unfollowed-and re-followed, and sum up all of the times you've been a follower to give you an accurate account of your follow activity.

## Why

People have been talking about Twitch's random/senseless "why am I not following you any more?!" mechanism that seems to make people UN-follow others on Twitch.

I'd like to build an improved !followage command that checks how long you've been following. Then:
- if I've never tracked how long you've been a follower, I write that value in SB's database
- if I HAVE tracked you before, and this new follow age is somehow LESS than a previous value, then surely you must have un-followed and then RE-followed, so add that to a list of follow-ages and then track this new value
- then return a SUM of all of those follow ages

eg, you follow me for 15 months, then somehow unfollow me, then re-follow me for 3 more months, I'd like to track that you've been following me for 18 months total.

Maybe the output could look like "You've followed iandouglas736 4 times for a total sum of 18 months, 3 weeks, and 2 days."

We'd probably need to use an average size of a "month" though purely based on days ... or sum up the days count back from today's date that many days, and then calculate how many months/etc that is.

The potential for abuse here is if we give out any kind of reward for "new" followers, we want to make sure we track that you have been a previous follower and NOT give you any "new" follower reward. (either that, or need to track which rewards you've been given and don't give you duplicates)

## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
