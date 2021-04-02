using System;

namespace scalus
{
    interface ICommandLineParser
    {
        IApplication Build(string[] args, Func<object,IApplication> applicationResolver);
    }
}
