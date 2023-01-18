using LovettSoftware.DgmlTestModeling;
using Microsoft.VisualStudio.GraphModel;
using NUnit.Framework;

namespace DgmlMonitorTest
{
    class UnitTest
    {
        [Test]
        public async Task Run()
        {
            // Connect with the DgmlTestMonitor tool window running inside a VS 2022 instance.
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
