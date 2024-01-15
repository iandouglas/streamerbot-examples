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
        DadJokeAPI dadjokeAPI = new DadJokeAPI();
        
        string response = dadjokeAPI.GenerateResponse();
        CPH.LogDebug(response);

        JokeResponse jokeResponse = JsonConvert.DeserializeObject<JokeResponse>(response);
        
        string myString = jokeResponse.Data.Joke;
        
        CPH.SetGlobalVar("DadJoke", myString, false);
 
        return true;
    }
}

class DadJokeAPI
{
    private string _endpoint = "https://us-central1-dadjokeapi.cloudfunctions.net/getRandomJoke";
    public DadJokeAPI()
    {
    }
    public string GenerateResponse()
    {
        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_endpoint);
        request.ContentType = "application/json";
        request.Method = "GET";

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

public class JokeResponse
{
    [JsonProperty("data")]
    public JokeData Data { get; set; }
}

public class JokeData
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("joke")]
    public string Joke { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }
}