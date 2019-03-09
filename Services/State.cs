using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Marauder.Objects;

namespace Marauder.Services
{
  public static class State
  {
    public static bool Debug = false;
    public static string Name;
    public static string PayloadName;
    public static string StagingId;
    public static string Password;
    public static int Sleep;
    public static double Jitter;
    public static int MaxAttempts;
    public static string LastTaskName;
    public static string InitialTransport;
    public static DateTime? ExpirationDate;
    public static List<TaskResult> ResultQueue = new List<TaskResult>();
    public static List<RunningTask> RunningTasks = new List<RunningTask>();
    public static TransportService TransportService;
    public static CryptoService CryptoService;
    public static CommandService CommandService;
  }
}
