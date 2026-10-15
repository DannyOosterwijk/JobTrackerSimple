using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJobTracker.Models
{
    public class JobProfile
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public string Tags { get; set; }
    }
}
