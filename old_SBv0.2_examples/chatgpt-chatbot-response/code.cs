using System;
using System.IO;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class CPHInline
{
    public bool Execute()
    {
        string apiKey = args["OpenAI-APIKey"].ToString();
        string model = args["model"].ToString();
        string content = args["behavior"].ToString();
        string temperature = args["temperature"].ToString();
        string maxTokens = args["maxTokens"].ToString();
        int maxMessages = Int32.Parse(args["maxMessages"].ToString());
        // get the message input from the arguments
        string messageInput = args["rawInput"].ToString();
        string user = args["user"].ToString();
        if (!CPH.ObsIsStreaming())
        {
            if (user != "iandouglas736")
            {
                return false;
            }
        }

        // create a ChatGPTAPI instance with your API key
        ChatGPTAPI chatGPT = new ChatGPTAPI(apiKey);
        // generate a response with the ChatGPTAPI
        string response = chatGPT.GenerateResponse(messageInput, model, maxTokens, content, temperature);
        // send the response to the chat
        Root root = JsonConvert.DeserializeObject<Root>(response);
        string myString = root.choices[0].message.content;
        CPH.LogInfo("GPT myString " + myString);
        string myStringCleaned0 = myString.Replace(System.Environment.NewLine, " ");
        string mystringCleaned1 = Regex.Replace(myStringCleaned0, @"\r\n?|\n", " ");
        string myStringCleaned2 = Regex.Replace(mystringCleaned1, @"[\r\n]+", " ");
        string unescapedString = Regex.Unescape(myStringCleaned2);
        string finalGPT = unescapedString.Trim();
        //CPH.SendMessage(finalGPT);
        //        CPH.SetGlobalVar("GPT", finalGPT, false);
        CPH.LogInfo("GPT Response:" + finalGPT);
        //        CPH.LogDebug(response);
        string[] splitWords = finalGPT.Split(' ');
        var chunks = ChunkString(finalGPT, 400);
        int messagesSent = 0;
        foreach (var chunk in chunks)
        {
            CPH.SendMessage($"{user}: {chunk}");
            messagesSent++;
            if (messagesSent >= maxMessages)
            {
                CPH.SendMessage($"{user}: ... sorry, truncating the response... it was too long to process");
                break;
            }
        }

        return true;
    }

    public static List<string> ChunkString(string str, int maxBytes)
    {
        List<string> chunks = new List<string>();
        StringBuilder currentChunk = new StringBuilder();
        int currentByteCount = 0;
        foreach (string word in str.Split(' '))
        {
            byte[] wordBytes = Encoding.UTF8.GetBytes(word + " ");
            if (currentByteCount + wordBytes.Length > maxBytes)
            {
                if (currentChunk.Length > 0)
                {
                    chunks.Add(currentChunk.ToString().Trim());
                }

                currentChunk.Clear();
                currentByteCount = 0;
            }

            currentChunk.Append(word + " ");
            currentByteCount += wordBytes.Length;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }
}

class ChatGPTAPI
{
    private string _apiKey;
    private string _endpoint = "https://api.openai.com/v1/chat/completions";
    public ChatGPTAPI(string apiKey)
    {
        _apiKey = apiKey;
    }

    public string GenerateResponse(string prompt, string model, string max_tokens, string behavior, string temperature)
    {
        // Create a request to the ChatGPT API
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint);
        request.Headers.Add("Authorization", "Bearer " + _apiKey);
        request.ContentType = "application/json";
        request.Method = "POST";
        // Build the request body
        string requestBody = "{\"model\": \"" + model + "\",\"max_tokens\": " + max_tokens + ", \"temperature\": " + temperature + ", \"messages\": [{\"role\": \"system\", \"content\": \" " + behavior + " \"}, {\"role\": \"user\", \"content\": \"" + prompt + "\"}]}";
        byte[] bytes = Encoding.UTF8.GetBytes(requestBody);
        request.ContentLength = bytes.Length;
        using (Stream requestStream = request.GetRequestStream())
        {
            requestStream.Write(bytes, 0, bytes.Length);
        }

        // Get the response from the ChatGPT API
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        string responseBody;
        using (Stream responseStream = response.GetResponseStream())
        {
            StreamReader reader = new StreamReader(responseStream, Encoding.UTF8);
            responseBody = reader.ReadToEnd();
        }

        return responseBody;
    }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class Choice
{
    public Message message { get; set; }
    public int index { get; set; }
    public object logprobs { get; set; }
    public string finish_reason { get; set; }
}

public class Root
{
    public string id { get; set; }
    public string @object { get; set; }
    public int created { get; set; }
    public string model { get; set; }
    public List<Choice> choices { get; set; }
    public Usage usage { get; set; }
}

public class Usage
{
    public int prompt_tokens { get; set; }
    public int completion_tokens { get; set; }
    public int total_tokens { get; set; }
}
