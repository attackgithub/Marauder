using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

using Marauder.Services;

namespace Marauder.Objects {
  public class RunningTask
  {
    public RunningTask() {
      Id = CryptoService.GenerateSecureString(4);
      CancellationTokenSource = new CancellationTokenSource();
      
    }
    public string Id;
    public string TaskName;
    public string Command;
    [JsonIgnore]
    public Task Task;
    [JsonIgnore]
    public CancellationTokenSource CancellationTokenSource;
  }
}