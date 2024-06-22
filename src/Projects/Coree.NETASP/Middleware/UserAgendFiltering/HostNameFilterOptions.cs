namespace Coree.NETASP.Middleware.UserAgentFiltering
{
    public class UserAgentFilterOptions
    {
        public string[]? Whitelist { get; set; }
        public string[]? Blacklist { get; set; }
    }
}
