using System;
using System.IO;
using System.Reflection;

using Marauder.Services;
using Marauder.Objects;

namespace Marauder
{
  static class Program
  {
    static void Main(string[] args)
    { 
#if !DEBUG
  Console.SetOut(TextWriter.Null);
  Console.SetError(TextWriter.Null);
#endif
      Marauder.Start();
    }
  }
}
