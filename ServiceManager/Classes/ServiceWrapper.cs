using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager.Classes
{
    public class ServiceWrapper
    {
        public ServiceWrapper()
        {
        }

        public ServiceWrapper(ServiceController sw)
        {
            this.ServiceName = sw.ServiceName;
        }

        public string ServiceName { get; set; }
        
        //public List<int> Groups { get; set; }
    }
}
