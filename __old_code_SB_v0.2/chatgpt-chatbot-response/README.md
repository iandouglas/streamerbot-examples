# ChatGPT chatbot response

This was based on another developer's work, House of Jacobs, https://www.youtube.com/watch?v=oXqxATLJva0 who originally got the code from THEBEASTBILLY, https://www.youtube.com/watch?v=XAZp8UTTEp8

I streamlined it, added more arguments, to make it more user friendly, and removed the Speaker.bot integration.

I also added protection to NOT run the code if your stream is offline, unless it's you running the command so you can test functionality. See the Troubleshooting section for more information on this.


## OpenAI Billing

**This chatbot responder WILL cost you real dollars to use.**

I recommend starting with about $10 in credit at OpenAI. You can get $10 in credit by signing up for an account at https://beta.openai.com/ and then clicking on your name in the top right corner, then clicking "My Settings", then clicking "Billing" in the left menu, then clicking "Add Funds" in the top right corner.

It's up to you, as a streamer, to determine how you want to use this. Right now I have it set with an `@gpt` chat trigger so my users can type `@gpt What is the square root of 49` but you may want to make this a channel point redemption or a bit redemption, and you'll need to make appropriate changes to the code to do that.

## Installation

You can use the content of [import_code.txt](import_code.txt) to import the Action and Command into Streamer.bot by using the Import button at the top of the UI.

You can change the Command from `@gpt` to something else if you like, or set your own trigger for a channel point redeem or bit redeem etc..

Once you import the Action, double click on the Execute Code sub-action, and click the "Find Refs" button at the bottom of the UI and then click the Compile button. If you get errors about missing assembly references, do the following:

Click on the "References" tab at the bottom of the window (you'll see tabs like "Compiling Log", "References", and "Settings"). Here, right-click somewhere in the panel and click "Add Reference". Then click the "Browse" button and navigate to the folder where you installed Streamer.bot. Then navigate to the "References" folder and select the following DLLs from your Microsoft .NET Framework folder (usually located at `C:\Windows\Microsoft.NET\Framework\v4.0.30319`)

- System.dll


## Arguments

1. `OpenAI-APIKey` 
  This is your OpenAI API Key. You can get an API key by signing up for an account at https://beta.openai.com/. You can create a new API key by clicking on your name in the top right corner, then clicking "My Settings", then clicking "API Keys" in the left menu, then clicking "New API Key" in the top right corner. Copy the API key and paste it into the "OpenAI-APIKey" argument in Streamer.bot.

2. `behavior`
  This is a behavioral prompt that will be prepended to every request made of ChatGPT. This is useful for setting the tone of the chatbot. For example, if you want the chatbot to be a "nice" chatbot, you can set this to "You are a nice person." and the chatbot will respond in a nice way. You could have fun with this like "You are a helpful AI assistant who responds like a pirate" or "You are a sarcastic chatbot who gives funny respones." You can also set this to a blank string if you want to have the chatbot respond in a neutral way.

3. `temperature` 
  This is a number between 0 and 1 that determines how "creative" the chatbot will be. The higher the number (eg, 0.9), the more creative the chatbot will be. The lower the number (eg, 0.1), the more "safe" the chatbot will be. I recommend starting with 0.5 and adjusting from there.

4. `maxMessages` 
  Each response from ChatGPT will be split into messages that will fit into Twitch Chat (about 400 bytes of text). This argument will set the maximum number of split messages that will be produced from the output before saying "the response was too long". If your `maxTokens` setting is low enough, this `maxMessages` may never be reached. I recommend starting with 3 and adjusting from there. Setting this too high can overwhelm your chat channel with text.

5. `maxTokens` 
  This is the maximum number of tokens that will be sent to ChatGPT. This value is approximately the number of words in the response, but is not exact. I recommend starting with 100 and adjusting from there. The higher you set this, the more text the chatbot will produce, but the more it will cost you in OpenAI credits. If the value is too low, the response from ChatGPT may get truncated.

6. `model` 
  This will default to `gpt-3.5-turbo` for text responses. You can also set this to `davinci` for more creative responses. The `davinci` model will cost you more in OpenAI credits. You can get a list of the models you have access to from https://beta.openai.com/docs/api-reference/retrieve-engine.


## Troubleshooting

There's a small block of code around line 25 of the C# script that will allow you to test this script in your own chat while your stream is offline. Right now this is set to my own username, but you can change this to be your own Twitch username. This will allow you to test the script without having to go live. Once you're happy with the script, you can remove this block of code. I leave it in so I can ask GPT brief questions in chat.

If you need help with this code, or anything else I've built in this GitHub repo, head over to [my Discord community](https://tig.fyi/discord) for help.
