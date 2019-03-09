using System;
using System.Collections.Generic;

namespace Marauder.Objects
{
  public class DecryptedMessageArgs : EventArgs
  {
    public List<AgentTask> AgentTasks;

    public DecryptedMessageArgs(List<AgentTask> agentTasks)
    {
      this.AgentTasks = agentTasks;
    }
  }

  public class MessageRecievedArgs : EventArgs
  {
    public string Message;

    public MessageRecievedArgs(string message)
    {
      this.Message = message;
    }
  }
}
