using System.Collections.Generic;

namespace Sulu.Dto
{

    public class SuluConfig
    {
        public List<ProtocolMapping> Protocols { get; set; }
        public List<ApplicationConfig> Applications { get; set; }
    }
}
