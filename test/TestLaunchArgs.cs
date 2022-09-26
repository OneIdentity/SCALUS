using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using OneIdentity.Scalus.UrlParser;
using static OneIdentity.Scalus.Dto.ParserConfigDefinitions;

namespace OneIdentity.Scalus.Test
{

    public class TestLaunchArgs
    {
        private string prot = "myprotocol";
        private string host = "thisismyhost";
        private string port = "333";
        private string path = "thisisapath";
        private string query = "a small query";
        private string fragment = "fragmented";
        private string vault = "myvaultaddress";
        private string token = "some token";
        private string targetuser = "enduser";
        private string targethost = "targethost";
        private string targetport = "333";
        private string fulluser;
        private string url;
        private string filename = Path.GetTempPath() + "tmpfile.rdp";

        private IDictionary<Token, string> SetupData()
        {
            fulluser = $"vaultaddress={vault}@token={token}@{targetuser}@{targethost}";
            url = $"{prot}://{fulluser}@{host}";


            return new Dictionary<Token, string>()
            {
                { Token.Protocol, prot },
                { Token.User, fulluser },
                { Token.Host, host },
                { Token.Port, port },
                { Token.Path, path },
                { Token.Query, query },
                { Token.Fragment, fragment },
                { Token.Vault, vault },
                { Token.Token, token },
                { Token.TargetUser, targetuser },
                { Token.TargetHost, targethost },
                { Token.TargetPort, targetport },
                { Token.OriginalUrl, url },
                { Token.RelativeUrl, $"{fulluser}@{host}" },
                { Token.GeneratedFile, filename },
                { Token.TempPath, Path.GetTempPath() },
                { Token.Home, "homedir" },
                { Token.AppData, "appdata/scalus"},
                { Token.AlternateShell, "myshell" },
                { Token.Remoteapplicationname, "myname" },
                { Token.Remoteapplicationprogram, "myprogram" },
                { Token.Account, "myaccount" },
                { Token.Asset, "myasset" },
                { Token.Remoteapplicationcmdline, "--cmd \"C:\\Apps\\TestRemoteApp\\TestRemoteApp.exe\" --args \"password={password} asset={asset} user={username}\" --enable-debug" }
            };
        }

        [Fact]
        public void Test1()
        {
            using (var parser = new DefaultSshUrlParser(new Dto.ParserConfig(), SetupData()))
            {
                var args = new List<string>()
            {
                $"%{Token.Protocol}%",
                $"%{Token.User}%",
                $"%{Token.Host}%",
                $"%{Token.Port}%",
                $"%{Token.Path}%",
                $"%{Token.Query}%",
                $"%{Token.Fragment}%",
                $"%{Token.Vault}%",
                $"%{Token.Token}%",
                $"%{Token.TargetUser}%",
                $"%{Token.TargetHost}%",
                $"%{Token.TargetPort}%",
                $"%{Token.OriginalUrl}%",
                $"%{Token.RelativeUrl}%",

                $"%{Token.GeneratedFile}%",
                $"%{Token.TempPath}%",
                $"%{Token.Home}%",
                $"%{Token.AppData}%",

                $"%{ Token.AlternateShell}%",
                $"%{ Token.Remoteapplicationname}%",
                $"%{ Token.Remoteapplicationprogram}%",
                $"%{ Token.Account}%",
                $"%{ Token.Asset}%",
                $"%{ Token.Remoteapplicationcmdline }%"
            };


                var newargs = parser.ReplaceTokens(args);
                var one = newargs.GetEnumerator();
                Assert.True(one.MoveNext());
                Assert.Equal(prot, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(fulluser, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(host, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(port, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(path, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(query, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(fragment, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(vault, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(token, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(targetuser, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(targethost, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(targetport, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(url, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal($"{fulluser}@{host}", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(filename, one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal(Path.GetTempPath(), one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("homedir", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("appdata/scalus", one.Current);


                Assert.True(one.MoveNext());
                Assert.Equal("myshell", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("myname", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("myprogram", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("myaccount", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("myasset", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("--cmd \"C:\\Apps\\TestRemoteApp\\TestRemoteApp.exe\" --args \"password={password} asset={asset} user={username}\" --enable-debug", one.Current);

                Assert.False(one.MoveNext());

                args = new List<string>
            {
                "%protocol",
                "user",
                "host%",
                "%host%%",
                "%host%hide"

            };
                newargs = parser.ReplaceTokens(args);
                one = newargs.GetEnumerator();
                Assert.True(one.MoveNext());
                Assert.Equal("%protocol", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("user", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("host%", one.Current);
                Assert.True(one.MoveNext());
                Assert.Equal("thisismyhost%", one.Current);

                Assert.True(one.MoveNext());
                Assert.Equal("thisismyhosthide", one.Current);
                Assert.False(one.MoveNext());
            }
        }
        [Fact]
        public void TestComplexParse()
        {
            var line = $"start %{Token.AlternateShell}% mid1 %{Token.AlternateShell}?shell1:shell2%  %{Token.Host}%  sometext%{Token.Host}?:% mid2 line%{Token.AlternateShell}?shell3:% %{Token.Host}?host1:%    %{Token.Host}?:host2%   %{Token.Host}?host3:host4%   %{Token.AlternateShell}?:shell4%";
            var exp1 = $"start %{Token.AlternateShell}% mid1 %{Token.AlternateShell}?shell1:shell2%  {host}  sometext mid2 line%{Token.AlternateShell}?shell3:% host1       host3   %{Token.AlternateShell}?:shell4%";
            var exp2 = $"start  mid1 shell2  {host}  sometext mid2 line host1       host3   shell4";
            var exp3 = $"start aaaaa mid1 shell1  {host}  sometext mid2 lineshell3 host1       host3   ";

            var newline = BaseParser.ReplaceToken(Token.User.ToString(), "abc", line);
            Assert.Equal(line, newline);

            newline = BaseParser.ReplaceToken(Token.Host.ToString(), host, line);
            Assert.Equal(exp1, newline);

            newline = BaseParser.ReplaceToken(Token.AlternateShell.ToString(), "", exp1);
            Assert.Equal(exp2, newline);

            newline = BaseParser.ReplaceToken(Token.AlternateShell.ToString(), "aaaaa", exp1);
            Assert.Equal(exp3, newline);

        }
        [Fact]
        public void Test2()
        {
            var list = ProtocolHandlerFactory.GetSupportedParsers();
            Assert.True(list.Count > 0);
            Assert.Contains("ssh", list);
            Assert.Contains("rdp", list);
            Assert.Contains("url", list);
            Assert.Contains("telnet", list);

        }

        [Fact]
        public void TestMacParsing()
        {
            var str = @"(
        {
        CFBundleURLName = ""com.oneidentity.scalus.macos"";
        CFBundleURLSchemes =         (
            ssh,
            rdp, 
            telnet
        );
        Another = ""sldkfjdk2"";
        Some = (
        One = ""two"";
        );
    }
)";
            var list = MacOSProtocolRegistrar.ParseList(str);
            Assert.True(list.Count > 0, $"first:{list[0]}, second:{list[1]}");
            Assert.Equal(3, list.Count);
            Assert.Equal("ssh", list[0]);
            Assert.Equal("rdp", list[1]);
            Assert.Equal("telnet", list[2]);

            var newstr = MacOSProtocolRegistrar.ConstructNewValue(list);

        }

        private static void CheckJson(string json, int expErrors)
        {
            var apiConfig = new ScalusApiConfiguration(json);
            apiConfig.Validate(json, true);
            Assert.Equal(expErrors, apiConfig.ValidationErrors.Count);
        }

        [Fact]
        public void TestJson()
        {
            var json = @"
               {
                    'Protocols':[
                        { 
                            'Protocol': 'one',
                            'AppId' : 'id'
                        }
                    ],
                    'Applications':[
                        {
                            'Id':'id',
                            'Name':'appname',
                            'Description':'desc',
                            'Platforms':['Windows','Linux','Mac'],
                            'Protocol':'one',
                            'Parser':{
                                'ParserId':'url',
                                'Options':['waitforexit'],
                                'UseDefaultTemplate':false,
                                'UseTemplateFile':'/path/tofile',
                                'PostProcessingExec':'path/toplugin',
                                'PostProcessingArgs':['arg1','arg2']
                            },
                            'Exec':'/path/tocommand',
                            'Args':['arg1','arg2']
                        }
                    ]
                }
";
            CheckJson(json, 0);

            json = @"
                   {
                        'Applications':[
                            {
                                'Id':'id',
                                'Platforms':['rubbish']
                            }
                        ]
                    }
";
            CheckJson(json, 1);

            json = @"
                   {
                        'Applications':[
                            {
                                'Id':'id',
                                'Platforms':[]
                            }
                        ]
                    }
";
            CheckJson(json, 1);

            json = @"
                   {
                        'Applications':[
                            {
                                'Id':'id',
                                'Platforms':['windows']
                            }
                        ]
                    }
";
            CheckJson(json, 1);

            json = @"
                   {
                        'Applications':[
                            {
                                'Id':'id',
                                'Platforms':['windows'],
                                'Protocol': 'one',
                                 'Parser':{
                                    'ParserId':'url',
                                },

                            }
                        ]
                    }
";
            CheckJson(json, 1);

            json = "{}";
            CheckJson(json, 0);

            json = "{'Protocols':[]}";
            CheckJson(json, 0);

            json = "{'Protocols':[{'Protocol':'one'}, {'Protocol':'one'}]}";
            CheckJson(json, 1);

            json = "{'Protocols':[{'Protocol':'one', 'AppId':'missing'}]}";
            CheckJson(json, 1);

            json = @"
                   {
                        'Applications':[
                            {
                                'Id':'id',
                                'Platforms':['windows'],
                                'Protocol': 'one',
                                'Exec':'one',
                                 'Parser':{
                                    'ParserId':'url',
                                },
                            },
                            {
                                'Id':'id',
                                'Platforms':['windows'],
                                'Protocol': 'one',
                                'Exec':'one',
                                 'Parser':{
                                    'ParserId':'url',
                                },
                            }
                        ]
                    }
";
            CheckJson(json, 1);

            json = "{'Protocols':[{'Protocol':'one$'}]}";
            CheckJson(json, 1);
        }

        [Fact]
        public void TestJson1()
        {
            var json = "{'Protocols':[{'Protocol':'one$'}]}";
            CheckJson(json, 1);
            json = "{'Protocols':[{'Protocol':'1one'}]}";
            CheckJson(json, 1);
            json = "{'Protocols':[{'Protocol':'one_'}]}";
            CheckJson(json, 1);
            json = "{'Protocols':[{'Protocol':'one-'}]}";
            CheckJson(json, 0);
            json = "{'Protocols':[{'Protocol':'one+'}]}";
            CheckJson(json, 0);
            json = "{'Protocols':[{'Protocol':'one.'}]}";
            CheckJson(json, 0);
            json = "{'Protocols':[{'Protocol':'one111'}]}";
            CheckJson(json, 0);




        }

        [Fact]
        public void TestInstalledJson()
        {
            var root = Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.FullName, "scripts");
            foreach (var dir in new string[] { "Win", "Linux", "Osx" })
            {
                var path = Path.Combine(root, Path.Combine(dir, "scalus.json"));
                Assert.True(File.Exists(path));
                var json = File.ReadAllText(path);
                CheckJson(json, 0);
            }
        }
    }
}
