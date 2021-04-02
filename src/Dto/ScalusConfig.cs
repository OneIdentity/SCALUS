using System.Collections.Generic;

namespace scalus.Dto
{

    public class ScalusConfig
    {
        public List<ProtocolMapping> Protocols { get; set; }
        public List<ApplicationConfig> Applications { get; set; }
    }
}
