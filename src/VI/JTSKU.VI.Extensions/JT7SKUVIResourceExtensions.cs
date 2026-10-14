using System;
using System.Collections.Generic;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace JTSKU.VI.Extensions
{
    public static class VIResourceExtensions
    {
        public static IResourceBuilder<VIResourssi> WithTallennusPaikka(this IResourceBuilder<VIResourssi> rakentaja, string? nimi = null)
        {
            return rakentaja.WithVolume(nimi,"/data");
        }
    }
}
