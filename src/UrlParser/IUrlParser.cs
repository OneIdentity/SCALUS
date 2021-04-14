using scalus.Dto;
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
    
    
    public interface IUrlParser : IDisposable
    {   
        IDictionary<Token,string> Parse(string url);
        void PostExecute(Process process);
        List<string> ReplaceTokens(List<string> args);
        void PreExecute(IOsServices services);
        Dictionary<Token, string> TokenDescription { get; }
    }
}
