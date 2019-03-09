using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marauder.Services
{
    public class Logging
    {
        public static void Write(string source, string message)
        {
            if (State.Debug) {
                string time = DateTime.Now.ToString("o");
                Console.WriteLine("({0}) [{1}] - {2}", time, source, message);
            }
        }
    }
}
