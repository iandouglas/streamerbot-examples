# Chat activity-gated promo scheduler

I'd like to build a reusable promo/announcement system for Streamer.bot that can post different messages into chat on different cadences, but only when the audience is actually talking.

## Why?

I have a mix of evergreen promos that I want to rotate through during a stream:

- follow my Twitch channel
- subscribe to my YouTube channel
- check out my career development newsletters
- sponsor or community callouts

The problem with a naive timer-based solution is that it will happily fill dead chat with bot messages. I only want these promos to show up when chat is active enough that the messages feel natural.

## Core behavior

Each promo item should have its own independent schedule, for example:

- Twitch follow reminder every 3 minutes
- YouTube subscribe reminder every 3 minutes
- Newsletter reminder every 5 minutes

But before any promo gets posted, the system should also check whether chat has been active since the last promo was sent.

## Recommended rules to explore

The scheduler should probably require **both** of these:

- at least `N` human chat messages since the last promo
- at least one human chat message within the last `M` seconds

Example defaults:

- `minimumMessagesSinceLastPromo = 8`
- `recentChatWindowSeconds = 90`

That should keep the chat cleaner when nobody is talking.

## Data model ideas

Store a promo catalog in memory with entries like:

```json
[
  {
    "Id": "follow",
    "Message": "Follow the channel on Twitch if you're having fun!",
    "IntervalSeconds": 180,
    "Enabled": true,
    "LastSentEpoch": 0
  },
  {
    "Id": "newsletter",
    "Message": "Want career development help? Check out my newsletter!",
    "IntervalSeconds": 300,
    "Enabled": true,
    "LastSentEpoch": 0
  }
]
```

Related chat activity globals could track:

- `chatActivityMessagesSinceLastPromo`
- `chatActivityLastNonBotMessageEpoch`
- optional `chatActivityUniqueUsersSinceLastPromo`
- optional `promoLastAnySentEpoch`

## Streamer.bot setup ideas

### Trigger 1: chat activity tracker

Use `Twitch -> Chat -> Chat Message` to track real audience activity.

This action should ignore:

- the broadcaster
- the bot account
- known bot/system/internal messages

It should increment counters or update activity globals whenever a real viewer chats.

### Trigger 2: promo scheduler timer

Use a single repeating timer every `10-15 seconds`.

That timer should:

1. load the promo catalog
2. determine which promos are currently due
3. check activity-gating rules
4. choose one promo to send
5. send the message as the bot account
6. update the promo's `LastSentEpoch`
7. reset `chatActivityMessagesSinceLastPromo`

## Selection strategy

Instead of strict round-robin, it may be better to choose the promo that is **most overdue**.

That lets different promo intervals coexist more naturally.

Still, I may also want a `promoNextIndex` style round-robin mode as an option later.

## Additional guardrails

Potential controls to include:

- minimum time gap between any two promos
- minimum viewer count before promos are allowed
- minimum unique chatters since last promo
- enable/disable promos by platform or stream type
- ability to pause promo posting during sensitive moments (raids, hype trains, serious conversations, etc.)

## Modern controls direction

This should be built as a modern, reusable control with:

- one catalog of promo entries
- one chat-activity tracker
- one scheduler timer
- global configuration for thresholds
- optional future integration with CSV, Google Sheets, or another external source

## Relationship to other ideas

This overlaps a bit with the `google-sheets-rotating-announcements` idea, but this project is focused first on **activity-aware local promo scheduling** rather than external data loading.

## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
