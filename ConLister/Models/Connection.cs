using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConLister.Models
{
    public struct Connection
    {
        public string SourceIP { get; set; }
        public string DestionationIP { get; set; }
        public int SourcePort { get; set; }
        public int DestinationPort { get; set; }
        public DateTime Seen { get; set; }
    }
}
