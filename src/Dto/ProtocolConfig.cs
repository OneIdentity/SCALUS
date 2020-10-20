namespace Sulu.Dto
{
    public class ProtocolConfig
    {
        public string Id { get; set; }
        public string Description { get; set; }
        public string Protocol { get; set; }
        public ParserConfig Parser { get; set; }
        public string Exec { get; set; }
        public string Args { get; set; }
    }
}
