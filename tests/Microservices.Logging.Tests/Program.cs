using System;
using System.Collections.Generic;
using System.Text;

namespace Microservices.Logging.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new InterfaceEFTests();
            t.TestEFGeneration();

        }
    }
}
