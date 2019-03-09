using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using Faction.Modules.Dotnet.Common;
using Marauder.Objects;
using Marauder.Services;

namespace Marauder.Commands
{
  class TasksCommand : Command
  {
    public override string Name { get { return "tasks"; } }
    public override CommandOutput Execute(Dictionary<string, string> Parameters = null)
    {
      CommandOutput output = new CommandOutput();
      output.Complete = true;
      output.Success = true;
      string task_id = "";

      if (Parameters.ContainsKey("Kill"))
      {
        task_id = Parameters["Kill"];
      }

      try
      {
        if (!String.IsNullOrEmpty(task_id) && Parameters.ContainsKey("Kill"))
        {
          RunningTask task = State.RunningTasks.Find(x => x.Id == task_id);
          task.CancellationTokenSource.Cancel();
          output.Message = $"Requested task {task_id} to cancel";
        }
        else {
          output.Message = JsonConvert.SerializeObject(State.RunningTasks);
        }
      }
      catch (Exception e)
      {
        output.Complete = true;
        output.Success = false;
        output.Message = e.Message;
      }
      return output;
    }
  }
}
