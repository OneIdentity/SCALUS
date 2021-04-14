using scalus.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace scalus.UrlParser
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true) ]  
    public class ParserName : Attribute  
    {  
        private string _name;   
        public ParserName(string name)  
        {  
            _name = name;  
        }       
        public string GetName()  
        {  
            return _name;  
        }  
    }  
    public enum Token
    {
        Protocol = 1,    
        User = 2,
        Host = 3,
        Port = 4,
        Path = 5,
        Query = 6,
        Fragment = 7,
        
        Vault = 8,
        Token = 9,
        TargetUser = 10,
        TargetHost = 11,
        TargetPort = 12,

        OriginalUrl =13,
        RelativeUrl = 14,
        GeneratedFile = 15,
        TempPath = 16,
        Home = 17
    };
    public enum ProcessingOptions{
       waitforinputidle =0,
        waitforexit=1,
        wait=2
    }

    public interface IUrlParser : IDisposable
    {   
        IDictionary<Token,string> Parse(string url);
        void PostExecute(Process process);
        List<string> ReplaceTokens(List<string> args);
        void PreExecute(IOsServices services);
        Dictionary<Token, string> TokenDescription { get; }
    }
}
