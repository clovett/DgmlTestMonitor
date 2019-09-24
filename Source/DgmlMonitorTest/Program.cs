using LovettSoftware.DgmlTestModeling;
using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DgmlMonitorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Program p = new Program();
            p.Run().Wait();
        }

        async Task Run()
        { 
            GraphStateWriter writer = new GraphStateWriter(Console.Out);
            await writer.Connect();

            string fileName = FindTestModel("Graph.dgml");
            Graph model = Graph.Load(fileName, DgmlTestModelSchema.Schema);
            await writer.LoadGraph(fileName);

            var start = model.Nodes.GetOrCreate("Foo");
            var node = start;
            do
            {
                await Task.Delay(100);
                await writer.NavigateToNode(node);
                var link = node.OutgoingLinks.FirstOrDefault();
                await Task.Delay(100);
                await writer.NavigateLink(link);
                node = link.Target;
            }
            while (node != start);
            await Task.Delay(100);
            await writer.NavigateToNode(node);
        }

        string FindTestModel(string filename)
        {
            string path = System.IO.Path.GetDirectoryName(new Uri(this.GetType().Assembly.Location).LocalPath);

            // walk up the directory tree.
            while (System.IO.Path.GetFileName(path) != "\\")
            {
                var test = System.IO.Path.Combine(path, filename);
                if (System.IO.File.Exists(test))
                {
                    return test;
                }
                path = System.IO.Path.GetDirectoryName(path);
            }
            return null;
        }
    }
}
