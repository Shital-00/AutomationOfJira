using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewJiraAutomation.Models
{
    internal class SprintResponse
    {
        public Sprint[] Values { get; set; }
    }

    internal class Sprint
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }


}
