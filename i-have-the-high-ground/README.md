# Elgato Streamdeck+ Knob Controls

Inspired by [Amish_Ace](https://twitch.tv/Amish_Ace)

Stream game idea.

When a user cheers 500 bits:

- if the timer since last redeem has expired:
    - display a chat message like "iandouglas736 has the high ground! They've held the high ground 42 times!"
    - optionally display an equivalent Star Wars video clip of Obi Wan Kenobi
    - if they were also the last person to cheer 500 bits, they continue to hold the high ground
    - if they were not the last person to cheer 500 bits, they are the new person with the high ground
- if the timer has NOT expired yet (someone is cheering bits too soon)
    - they see a message like "Sorry, but iandouglas736 has the high ground -- but YOU WERE THE CHOSEN ONE!"

Arguments to build:
- how long between redeems
- how many bits to trigger the game
- another action name, if the streamer has one, to play the "high ground" video

Chat commands to build:

- `!highground` -- show who currently has the high ground and how often they've held the high ground
- `!hgleaders` -- top list of who has held the high ground since last reset

Streamer controls to build:

- a means to reset the standings; without a reset, new players are less likely to engage in the game if they can never get to the #1 position
    - could this be done automatically on a 30-day rolling basis?

## Coming Soon

If you'd like this built sooner than later, please visit [my Discord community](https://tig.fyi/discord) and upvote this in the `#streamerbot-ideas` channel.
