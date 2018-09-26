using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nec.Nebula.Test
{
    class TestUtils
    {
        public const string TenantId = "tenant";
        public const string AppId = "appid";
        public const string AppKey = "appkey";
        public const string EndpointUri = "http://api.example.com";

        public static void Init()
        {
            var service = NbService.GetInstance();

            service.TenantId = TenantId;
            service.AppId = AppId;
            service.AppKey = AppKey;
            service.EndpointUrl = EndpointUri;
        }
    }
}
