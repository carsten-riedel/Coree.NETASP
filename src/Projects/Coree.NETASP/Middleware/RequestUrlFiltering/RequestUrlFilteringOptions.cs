namespace Coree.NETASP.Middleware.RequestUrlFiltering
{
    public class RequestUrlFilteringOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
    }
}
