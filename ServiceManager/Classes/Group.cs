using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager.Classes
{
    public class Group
    {
        public string Name { get; set; }

        public List<ServiceWrapper> Services { get; set; }

        public int ID { get; set; }

        public Group() {
            Services = new List<ServiceWrapper>();
        }

        public Group(string name): this() {
            this.Name = name;
        }
    }
}
