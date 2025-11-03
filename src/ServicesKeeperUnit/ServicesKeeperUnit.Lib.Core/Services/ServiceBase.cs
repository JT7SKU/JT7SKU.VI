using System;
using System.Collections.Generic;
using System.Text;

namespace ServicesKeeperUnit.Lib.Core.Services
{
    public abstract class ServiceBase
    {
        public readonly string HEADER_SUB_KEY = "Ocp-Apim-Subscription-Key";
        public readonly int MAX_QUERY_LENGHT = 1000;
        public IDictionary<string, string> RequestHeaders { get; protected set; }
        public string BaseServiceUrl { get; protected set; }
    }
}
