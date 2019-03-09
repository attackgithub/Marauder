using System;
using System.Collections.Generic;

using Marauder.Services;

namespace Marauder.Objects
{
  public partial class EncryptedMessage
  {
    public string AgentName { get; set; }
    public string IV { get; set; }
    public string HMAC { get; set; }
    public string Message { get; set; }

    public EncryptedMessage() { }
    public EncryptedMessage(Dictionary<string, string> StagingDictionary) {
      AgentName = StagingDictionary["AgentName"];
      IV = StagingDictionary["IV"];
      HMAC = StagingDictionary["HMAC"];
      Message = StagingDictionary["Message"];
    }
  } 
}
