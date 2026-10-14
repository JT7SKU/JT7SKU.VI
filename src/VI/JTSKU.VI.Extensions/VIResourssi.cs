using Aspire.Hosting.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace JTSKU.VI.Extensions
{
    public class VIResourssi([ResourceName]string nimi) : ContainerResource(nimi)
    {
        private const string OletusKayttajaNimi = "n7-palvelu";
    }
}
