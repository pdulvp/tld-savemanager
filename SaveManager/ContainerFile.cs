using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SaveManager
{
    public class ContainerFile
    {
        public DateTime Timestamp { get; set; }

        public string Date
        {
            get { return Timestamp.ToString("yyyy/MM/dd-hh:mm:ss"); }
        }

        public string Filename { get; set; }
        public string Summary { get; set; }
        public bool Locked { get; set; }
        public bool Deleted { get; set; }
    }
}
