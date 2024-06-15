using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Options;

namespace Coree.NETASP.Extensions.Options.KestrelServer
{
    //push
    public static partial class OptionsKestrelServerOptionsExtensions
    {
        public static void dd(this KestrelServerOptions kestrelServerOptions)
        {
            kestrelServerOptions.ListenAnyIP(80);
        }

        //sd
        //ikggu


    }


}
