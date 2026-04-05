using System;
using System.Text;
using System.Linq;
using Artemis_IL.Handlers;

namespace Artemis_IL
{
    public static class Globals
    {
        /// <summary>
        /// Null console for the Virtual Machine default implementation.
        /// The runtime would contain the override Console implementation instead.  
        /// </summary>
        /// <returns>An empty console that provides no I/O</returns>
        public static VConsole console = new NullConsole();
        public static bool DebugMode = true;
    }
}