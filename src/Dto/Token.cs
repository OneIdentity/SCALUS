

using System.Collections.Generic;

namespace scalus.Dto
{
    public class ParserConfigDefinitions {
    
        //The set of tokens supported by the parsers. A parser is not required to support all tokens
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
            Home = 17,
            AppData = 18
        };

        //processing options supported
        public enum ProcessingOptions{
            waitforinputidle =0,
            waitforexit=1,
            //wait for a specified number of seconds by using e.g. "wait:60"
            wait=2
        }


        public static Dictionary<Token, string> TokenDescription { get; } = new Dictionary<Token, string>
        {
            {
                Token.OriginalUrl,
                "The full URL string. This token is available even if it is not a valid URL, e.g. 'rdp://full+address=s:address&username=s:user'."
            },
            {Token.RelativeUrl, "The URL string without the protocol, e.g. 'full+address=s:address&username=s:user'."},
            {Token.Protocol, "The protocol in use, eg. scheme part of the URL, e.g. 'rdp'"},
            {
                Token.User,
                "The user (userinfo of the URL). For Safeguard URLs, this will contain the auth token information, e.g. 'vaultaddress~hostname%token~tokenstring%user1@targethost."
            },
            {Token.Host, "The hostname or IP address that the application will connect to."},
            {Token.Port, "The port number that the application will connect to."},
            {Token.Path, "The path part of a standard URL."},
            {Token.Query, "The query of a standard URL."},
            {Token.Fragment, "The fragment part of a standard URL."},
            {Token.Vault, "The hostname or IP address of the Safeguard vault, extracted from a Safeguard URL."},
            {Token.Token, "The token part of a Safeguard URL."},
            {Token.TargetUser, "The target username part of a Safeguard URL."},
            {Token.TargetHost, "The target host name or IP address part of a Safeguard URL."},
            {Token.TargetPort, "The target port part of a Safeguard URL."},
            {Token.GeneratedFile,"The generated file that will be passed to the application, if UseDefaultTemplate is true or UseTemplateFile is configured. The file extension will be set to that of the template (if provided), or determined by the parser."},
            {Token.TempPath, "The user's temp directory on this platform."},
            {Token.Home, "The user's home directory on this platform."},
            {Token.AppData, "The user's local application directory on this platform."}
        };
    }
}
