using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Consul.Persistence.Sample
{
    public class TestAppConfig
    {
        public string Name { get; set; }

        public string Version { get; set; }

        public TestAppType AppType { get; set; }
    }

    public class TestAppType
    {
        public string Name { get; set; }

        public string Node { get; set; }
    }
}
