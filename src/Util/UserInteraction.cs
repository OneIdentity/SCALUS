using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu.Util
{
    class UserInteraction : IUserInteraction
    {
        public void Error(string error)
        {
            Console.Error.WriteLine(error);
        }

        public void Message(string message)
        {
            Console.WriteLine(message);
        }
    }
}
