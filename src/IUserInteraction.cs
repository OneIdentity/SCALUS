using System;
using System.Collections.Generic;
using System.Text;

namespace Sulu
{
    interface IUserInteraction
    {
        void Message(string message);
        void Error(string error);
    }
}
