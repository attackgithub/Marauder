using System;
using System.Text;
using System.Collections.Generic;

namespace Marauder.Objects {
  public class AgentTask
  {
    public string Name;
    public string AgentName;

    public string Action;
    public string Command;

    public AgentTask(Dictionary<string, string> TaskDictonary) {
      Name = TaskDictonary["Name"];
      AgentName = TaskDictonary["AgentName"];
      Action = TaskDictonary["Action"];
      Command = TaskDictonary["Command"];
    }

    public AgentTask() { }
  }
}