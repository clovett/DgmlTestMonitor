using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VTeam.DgmlTestMonitor
{
    public class LogViewModel
    {
        public GraphObject Object { get; set; }
    }

    public class NodeViewModel : LogViewModel
    {
        public string Label { get; set; }
        public string Id { get; set; }
    }

    public class LinkViewModel : LogViewModel
    {
        public string SourceId { get; set; }
        public string Source { get; set; }

        public string TargetId { get; set; }
        public string Target { get; set; }
    }
}
