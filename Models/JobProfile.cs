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
        public List<SearchResult> History { get; set; } = new List<SearchResult>();

        public SearchResult GetLastResult() {  return (History != null && History.Count != 0) ? History[History.Count - 1] : null;}
    }
}
