using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleJobTracker.Models
{
    public class EmailSettings
    {
        public string Adress {  get; set; }
        public bool SendChecked { get; set; }
        public bool BubbleChecked { get; set; }
    }
}
