using System;

namespace scalus.Util
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

    
    //class GuiUserInteraction : IUserInteraction
    //{
    //    public void Error(string error)
    //    {
    //        System.Windows.Forms.MessageBox.Show(error);
    //    }

    //    public void Message(string message)
    //    {
    //        System.Windows.Forms.MessageBox.Show(message);
    //    }
    //}
}
