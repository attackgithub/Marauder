using System;
using System.Collections.Generic;
using Faction.Modules.Dotnet.Common;

namespace Marauder.Objects {

  public class TaskResult
  {
    public TaskResult() {
      IOCs = new HashSet<IOC>();
    }
    public string TaskName;
    public bool Success;
    public bool Complete;
    public string Message;
    public string Type { get; set; }
    public string Content { get; set; }
    public string ContentId { get; set; }
    public ICollection<IOC> IOCs;

  }
}