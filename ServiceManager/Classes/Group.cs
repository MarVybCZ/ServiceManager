using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager.Classes
{
    class Group
    {
        public string Name { get; set; }

        public List<ServiceWrapper> Services { get; set; }

        public int ID { get; set; }
    }
}
