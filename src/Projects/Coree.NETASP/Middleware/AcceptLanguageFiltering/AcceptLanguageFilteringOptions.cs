namespace Coree.NETASP.Middleware.AcceptLanguageFiltering
{
    public class AcceptLanguageFilteringOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
    }
}
