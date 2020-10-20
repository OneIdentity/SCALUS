using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu.Dto
{

    public class SuluConfig
    {
        public List<ProtocolMapping> Map { get; set; }
        public List<ProtocolConfig> Protocols { get; set; }
    }
}
