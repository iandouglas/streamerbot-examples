# Get a funny joke from Ian's dad-a-base of Dad Jokes

This setup will retrieve a random joke from an API that Ian wrote and constantly adds to.

## Setup

Import the code from `import_code.txt` into Streamer.bot 0.2.x using the Import feature. This will create a new action called "Dad Joke API to Twitch Chat" with sub-actions that retrieve a joke, parse it, and send it to Twitch Chat, which you can easily reformat.

## Compiling

After you import the code, double-click on the "Execute Code" sub-action and click the "Compile" button. If you get a compiler error, click on the "Find Refs" button and try to "Compile" again.

If it still fails, you'll need to click on the "References" tab in the interface, right click somewhere in the panel and choose "Add reference from file", navigate to where you have Streamer.bot installed, and choose the "Newtwonsoft.Json.dll" file. Then try to compile again.

If compiling succeeds, click the "Save and Compile" button.

## Pledge of Clean Humor

The only jokes in this API are jokes that I have been telling my own kids for many years. While there may be some mild crude humor about farts or poking fun at the idea of death, I have completely avoided any known-to-me racial humor or derogatory terms.

If you find a joke that is inappropriate, please contact me and let me know why, and I will consider removing it. I won't promise removal.

## Using it

The imported code will look for two triggers: `!dadjoke` as a chat command, and a channel point redemption. You can remove either of these triggers and set it up however you like with whichever permissions you need.

## Copyright on use

These jokes are found all over the internet, some were written or modified by me, and attribution has never been tracked. If you are the verifiable author of one of the jokes in the API, contact me and I will alter the joke to include attribution to you which will be part of the joke content itself so it will always be displayed.

## Rate Limiting

I reserve the right to rate limit the API at any time without notice.

## Troubleshooting

If you have trouble setting this up, head over to [my Discord community](https://tig.fyi/discord) for help.

