using System.Collections.Generic;
using System.Diagnostics;

namespace scalus.Platform
{
    public interface IOsServices
    {
        Process OpenDefault(string file);

        Process Execute(string binary, IEnumerable<string> args);

        /// <summary>
        /// Open a text editor to display message
        /// </summary>
        void OpenText(string message);

        bool IsAdministrator();
        int Execute(string command, IEnumerable<string> args, out string stdOut, out string stdErr);
    }
}
