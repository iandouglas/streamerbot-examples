# Streamer.bot Client Quick Reference

## Common snippets

- `new StreamerbotClient(opts)` — Create and optionally auto-connect.
- `client.on('Twitch.ChatMessage', cb)` — Subscribe to an event with auto-subscription.
- `client.subscribe({ Twitch: ['ChatMessage'] })` — Manual subscription.
- `client.unsubscribe('*')` — Unsubscribe from all events.
- `client.getActions()` — List all Streamer.bot actions.
- `client.doAction(id, args, opts)` — Execute an action; supports customEventResponse.
- `client.getBroadcaster()` — Fetch connected broadcaster platform info.
- `client.getActiveViewers()` — Fetch current Twitch/YouTube active viewers.
- `client.getGlobals() / getGlobal(name)` — Fetch persisted or non-persisted globals.
- `client.sendMessage(platform, message, opts)` — Send chat; requires authenticated WebSocket.
- `client.executeCodeTrigger(name, args)` — Fire a custom C# trigger from the client.
- `client.getInfo()` — Get Streamer.bot instance info.

## Minimal browser overlay

```html
<!DOCTYPE html>
<html>
  <head>
    <script src="https://cdn.jsdelivr.net/npm/@streamerbot/client/dist/streamerbot-client.js"></script>
  </head>
  <body>
    <div id="chat"></div>
    <script>
      const client = new StreamerbotClient();
      client.on('Twitch.ChatMessage', ({ data }) => {
        document.getElementById('chat').innerHTML +=
          `<div><b>${data.message.displayName}</b>: ${data.message.message}</div>`;
      });
    </script>
  </body>
</html>
```

## Event source quick list

- `Application` — 6 types
- `Command` — 2 types
- `CrowdControl` — 9 types
- `Custom` — 2 types
- `DonorDrive` — 3 types
- `Elgato` — 90 types
- `FileTail` — 1 types
- `FileWatcher` — 4 types
- `General` — 1 types
- `HypeRate` — 4 types
- `Inputs` — 2 types
- `Kofi` — 5 types
- `Midi` — 1 types
- `Misc` — 13 types
- `Obs` — 9 types
- `Patreon` — 5 types
- `Pulsoid` — 1 types
- `Quote` — 2 types
- `Raw` — 3 types
- `Shopify` — 2 types
- `SpeechToText` — 2 types
- `StreamDeck` — 4 types
- `StreamElements` — 5 types
- `Streamlabs` — 6 types
- `TipeeeStream` — 1 types
- `TreatStream` — 4 types
- `Twitch` — 136 types
- `VTubeStudio` — 11 types
- `WebsocketClient` — 3 types
- `WebsocketCustomServer` — 3 types
- `YouTube` — 29 types

## Important event sources for overlays/games

### Twitch
- `Twitch.Follow`
- `Twitch.Cheer`
- `Twitch.Sub`
- `Twitch.ReSub`
- `Twitch.GiftSub`
- `Twitch.GiftBomb`
- `Twitch.Raid`
- `Twitch.HypeTrainStart`
- `Twitch.HypeTrainUpdate`
- `Twitch.HypeTrainLevelUp`
- `Twitch.HypeTrainEnd`
- `Twitch.RewardRedemption`
- `Twitch.RewardCreated`
- `Twitch.RewardUpdated`
- `Twitch.RewardDeleted`
- `Twitch.CommunityGoalContribution`
- `Twitch.CommunityGoalEnded`
- `Twitch.StreamUpdate`
- `Twitch.Whisper`
- `Twitch.FirstWord`
- `Twitch.SubCounterRollover`
- `Twitch.BroadcastUpdate`
- `Twitch.StreamUpdateGameOnConnect`
- `Twitch.PresentViewers`
- `Twitch.PollCreated`
- `Twitch.PollUpdated`
- `Twitch.PollCompleted`
- `Twitch.PredictionCreated`
- `Twitch.PredictionUpdated`
- `Twitch.PredictionCompleted`
- `Twitch.PredictionCanceled`
- `Twitch.PredictionLocked`
- `Twitch.ChatMessage`
- `Twitch.ChatMessageDeleted`
- `Twitch.UserTimedOut`
- `Twitch.UserBanned`
- `Twitch.Announcement`
- `Twitch.AdRun`
- `Twitch.BotWhisper`
- `Twitch.CharityDonation`
- `Twitch.CharityCompleted`
- `Twitch.CoinCheer`
- `Twitch.ShoutoutCreated`
- `Twitch.UserUntimedOut`
- `Twitch.CharityStarted`
- `Twitch.CharityProgress`
- `Twitch.GoalBegin`
- `Twitch.GoalProgress`
- `Twitch.GoalEnd`
- `Twitch.ShieldModeBegin`
- `Twitch.ShieldModeEnd`
- `Twitch.StreamOnline`
- `Twitch.StreamOffline`
- `Twitch.ShoutoutReceived`
- `Twitch.ChatCleared`
- `Twitch.RaidStart`
- `Twitch.RaidSend`
- `Twitch.RaidCancelled`
- `Twitch.PollTerminated`
- `Twitch.PyramidSuccess`
- `Twitch.PyramidBroken`
- `Twitch.ViewerCountUpdate`
- `Twitch.GuestStarSessionBegin`
- `Twitch.GuestStarSessionEnd`
- `Twitch.GuestStarGuestUpdate`
- `Twitch.GuestStarSlotUpdate`
- `Twitch.GuestStarSettingsUpdate`
- `Twitch.HypeChat`
- `Twitch.RewardRedemptionUpdated`
- `Twitch.HypeChatLevel`
- `Twitch.BroadcasterAuthenticated`
- `Twitch.BroadcasterChatConnected`
- `Twitch.BroadcasterChatDisconnected`
- `Twitch.BroadcasterEventSubConnected`
- `Twitch.BroadcasterEventSubDisconnected`
- `Twitch.SevenTVEmoteAdded`
- `Twitch.SevenTVEmoteRemoved`
- `Twitch.BetterTTVEmoteAdded`
- `Twitch.BetterTTVEmoteRemoved`
- `Twitch.BotEventSubConnected`
- `Twitch.BotEventSubDisconnected`
- `Twitch.UpcomingAd`
- `Twitch.ModeratorAdded`
- `Twitch.ModeratorRemoved`
- `Twitch.VipAdded`
- `Twitch.VipRemoved`
- `Twitch.UserUnbanned`
- `Twitch.UnbanRequestApproved`
- `Twitch.UnbanRequestDenied`
- `Twitch.AutomaticRewardRedemption`
- `Twitch.UnbanRequestCreated`
- `Twitch.PowerUp`
- `Twitch.ChatEmoteModeOn`
- `Twitch.ChatEmoteModeOff`
- `Twitch.ChatFollowerModeOn`
- `Twitch.ChatFollowerModeOff`
- `Twitch.ChatFollowerModeChanged`
- `Twitch.ChatSlowModeOn`
- `Twitch.ChatSlowModeOff`
- `Twitch.ChatSlowModeChanged`
- `Twitch.ChatSubscriberModeOn`
- `Twitch.ChatSubscriberModeOff`
- `Twitch.ChatUniqueModeOn`
- `Twitch.ChatUniqueModeOff`
- `Twitch.AutoModMessageHeld`
- `Twitch.AutoModMessageUpdate`
- `Twitch.BlockedTermsAdded`
- `Twitch.BlockedTermsDeleted`
- `Twitch.WarnedUser`
- `Twitch.SuspiciousUserUpdate`
- `Twitch.PermittedTermsAdded`
- `Twitch.PermittedTermsDeleted`
- `Twitch.WarningAcknowledged`
- `Twitch.WatchStreak`
- `Twitch.PollArchived`
- `Twitch.SharedChatSessionBegin`
- `Twitch.SharedChatSessionUpdate`
- `Twitch.SharedChatSessionEnd`
- `Twitch.PrimePaidUpgrade`
- `Twitch.PayItForward`
- `Twitch.GiftPaidUpgrade`
- `Twitch.BitsBadgeTier`
- `Twitch.SharedChatAnnouncement`
- `Twitch.SharedChatRaid`
- `Twitch.SharedChatPrimePaidUpgrade`
- `Twitch.SharedChatGiftPaidUpgrade`
- `Twitch.SharedChatPayItForward`
- `Twitch.SharedChatSub`
- `Twitch.SharedChatResub`
- `Twitch.SharedChatSubGift`
- `Twitch.SharedChatCommunitySubGift`
- `Twitch.SharedChatUserBanned`
- `Twitch.SharedChatUserUnbanned`
- `Twitch.SharedChatUserTimedout`
- `Twitch.SharedChatUserUntimedout`
- `Twitch.SharedChatMessageDeleted`

### YouTube
- `YouTube.BroadcastStarted`
- `YouTube.BroadcastEnded`
- `YouTube.Message`
- `YouTube.MessageDeleted`
- `YouTube.UserBanned`
- `YouTube.SuperChat`
- `YouTube.SuperSticker`
- `YouTube.NewSponsor`
- `YouTube.MemberMileStone`
- `YouTube.NewSponsorOnlyStarted`
- `YouTube.NewSponsorOnlyEnded`
- `YouTube.StatisticsUpdated`
- `YouTube.BroadcastUpdated`
- `YouTube.MembershipGift`
- `YouTube.GiftMembershipReceived`
- `YouTube.FirstWords`
- `YouTube.PresentViewers`
- `YouTube.NewSubscriber`
- `YouTube.BroadcastMonitoringStarted`
- `YouTube.BroadcastMonitoringEnded`
- `YouTube.BroadcastAdded`
- `YouTube.BroadcastRemoved`
- `YouTube.SevenTVEmoteAdded`
- `YouTube.SevenTVEmoteRemoved`
- `YouTube.BetterTTVEmoteAdded`
- `YouTube.BetterTTVEmoteRemoved`
- `YouTube.PollClosed`
- `YouTube.PollStarted`
- `YouTube.PollUpdated`

### Obs
- `Obs.Connected`
- `Obs.Disconnected`
- `Obs.Event`
- `Obs.SceneChanged`
- `Obs.StreamingStarted`
- `Obs.StreamingStopped`
- `Obs.RecordingStarted`
- `Obs.RecordingStopped`
- `Obs.VendorEvent`

### Misc
- `Misc.TimedAction`
- `Misc.Test`
- `Misc.ProcessStarted`
- `Misc.ProcessStopped`
- `Misc.ChatWindowAction`
- `Misc.StreamerbotStarted`
- `Misc.StreamerbotExiting`
- `Misc.ToastActivation`
- `Misc.GlobalVariableUpdated`
- `Misc.UserGlobalVariableUpdated`
- `Misc.GlobalVariableCreated`
- `Misc.GlobalVariableDeleted`
- `Misc.ApplicationImport`

### General
- `General.Custom`

### Custom
- `Custom.Event`
- `Custom.CodeEvent`

### Streamlabs
- `Streamlabs.Donation`
- `Streamlabs.Merchandise`
- `Streamlabs.Connected`
- `Streamlabs.Disconnected`
- `Streamlabs.CharityDonation`
- `Streamlabs.Authenticated`

### StreamElements
- `StreamElements.Tip`
- `StreamElements.Merch`
- `StreamElements.Connected`
- `StreamElements.Disconnected`
- `StreamElements.Authenticated`

### CrowdControl
- `CrowdControl.GameSessionStart`
- `CrowdControl.GameSessionEnd`
- `CrowdControl.EffectRequest`
- `CrowdControl.EffectSuccess`
- `CrowdControl.EffectFailure`
- `CrowdControl.TimedEffectStarted`
- `CrowdControl.TimedEffectEnded`
- `CrowdControl.TimedEffectUpdated`
- `CrowdControl.CoinExchange`
