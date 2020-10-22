using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sulu.Dto
{

    public class SuluConfig
    {
        public List<ProtocolMapping> Protocols { get; set; }
        public List<ApplicationConfig> Applications { get; set; }
    }
}
