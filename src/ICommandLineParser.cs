using System;

namespace Sulu
{
    interface ICommandLineParser
    {
        IApplication Build(string[] args, Func<object,IApplication> applicationResolver);
    }
}
