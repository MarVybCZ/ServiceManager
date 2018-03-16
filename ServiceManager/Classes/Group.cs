using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager.Classes
{
    public class Group
    {
        public string Name { get; set; }

        public List<ServiceWrapper> Services { get; set; }

        public int ID { get; set; }

        public Group()
        {
            Services = new List<ServiceWrapper>();
        }

        public Group(string name) : this()
        {
            this.Name = name;
        }

        public void AddService(ServiceWrapper service)
        {
            this.Services.Add(service);
        }

        public void AddServiceRange(List<ServiceWrapper> services)
        {
            this.Services.AddRange(services);
        }

        internal void AddServiceRange(IList services)
        {
            foreach (var sw in services)
            {
                if (sw is ServiceWrapper)
                    this.AddService((ServiceWrapper)sw);

                if (sw is ServiceController)
                    this.AddService(new ServiceWrapper((ServiceController)sw));
            }
        }
    }
}
