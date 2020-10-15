using System;

namespace Sulu
{
    interface IApplicationBuilder
    {
        IApplication Build(string[] args, Func<object,IApplication> applicationResolver);
    }
}
