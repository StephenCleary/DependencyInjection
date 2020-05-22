using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

internal static class Utility
{
    public static ServiceProviderOptions SafeServiceProviderOptions = new ServiceProviderOptions
    {
#if !NETCOREAPP2_1
        ValidateOnBuild = true,
#endif
        ValidateScopes = true,
    };
}
