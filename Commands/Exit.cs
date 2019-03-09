using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

using Faction.Modules.Dotnet.Common;
using Marauder.Objects;
using Marauder.Services;

namespace Marauder.Commands
{
  class ExitCommand : Command
  {
    public override string Name { get { return "exit"; } }
    public override CommandOutput Execute(Dictionary<string, string> Parameters = null)
    {
      Environment.Exit(0);

      // Much like life its self, none of this matters.
      CommandOutput output = new CommandOutput();
      output.Complete = true;
      output.Success = true;
      output.Message = "Agent has exited";
      return output;
    }
  }
}
