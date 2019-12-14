using Microsoft.Coyote.SmartSockets;
using Microsoft.VisualStudio.GraphModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LovettSoftware.DgmlTestModeling
{
    /// <summary>
    /// This class serializes graph node transitions over a named pipe to a GraphStateReader.
    /// </summary>
    public class GraphStateWriter
    {
        SmartSocketClient pipe;
        AutoResetEvent connectedEvent = new AutoResetEvent(false);
        internal static string LoadGraphPrefix = "LoadGraph:";
        internal static string NavigateToNodePrefix = "NavigateToNode:";
        internal static string NavigateLinkPrefix = "NavigateLink:";
        internal static int DefaultPort = 18777;
        private CancellationTokenSource source;
        HashSet<GraphNodeId> createdNodes = new HashSet<GraphNodeId>();
        HashSet<string> createdLinks = new HashSet<string>();

        /// <summary>
        /// Construct new graph state writer
        /// </summary>
        /// <param name="log">Separate log file in addition to the named pipe</param>
        public GraphStateWriter(TextWriter log)
        {
        }

        /// <summary>
        /// Connect to the server so we can start sending messages
        /// </summary>
        /// <returns></returns>
        public async Task Connect()
        {
            this.source = new CancellationTokenSource();
            var resolver = new SmartSocketTypeResolver(typeof(Message),
                                                       typeof(ConnectedMessage),
                                                       typeof(ClearTextMessage),
                                                       typeof(LoadGraphMessage),
                                                       typeof(CreateNodeMessage),
                                                       typeof(CreateLinkMessage),
                                                       typeof(NavigateNodeMessage),
                                                       typeof(NavigateLinkMessage));
            this.pipe = await SmartSocketClient.FindServerAsync("DgmlTestMonitor", "GraphStateWriter", resolver, source.Token);
        }

        /// <summary>
        /// Instruct client to load the given graph.
        /// </summary>
        /// <param name="path">Full path to .dgml file</param>
        public async Task LoadGraph(string path)
        {
            await pipe.SendReceiveAsync(new LoadGraphMessage(path));
        }

        private void GetParentChain(GraphNode node, List<GraphNode> parents)
        {
            foreach (GraphNode parent in node.GetSources(GraphCommonSchema.Contains))
            {
                parents.Insert(0, parent);
                GetParentChain(parent, parents);
                break;
            }
        }

        private async Task CreateParentChain(GraphNode node)
        {
            if (!createdNodes.Contains(node.Id))
            {
                List<GraphNode> chain = new List<GraphNode>();
                GetParentChain(node, chain);
                chain.Add(node);

                // Now recreate this parent chain in top down order.
                GraphNode p = null;
                foreach (GraphNode g in chain)
                {
                    if (!createdNodes.Contains(g.Id))
                    {
                        createdNodes.Add(g.Id);                        
                        GraphCategory c = g.Categories.FirstOrDefault();
                        await pipe.SendReceiveAsync(new CreateNodeMessage(g.Id.ToString(), g.Label, c?.Id, g.IsGroup, p?.Id.ToString()));
                    }
                    p = g;
                }
            }
        }

        /// <summary>
        /// Instruct client to navigate to the given node
        /// </summary>
        /// <param name="node">A GraphNode object belonging to the graph loaded in LoadGraph</param>
        public async Task NavigateToNode(GraphNode node)
        {
            await CreateParentChain(node);
            await pipe.SendReceiveAsync(new NavigateNodeMessage(node.Id.ToString()));
        }

        /// <summary>
        /// Instruct client to navigate the given link
        /// </summary>
        /// <param name="link">A GraphLink object belonging to the graph loaded in LoadGraph</param>
        public async Task NavigateLink(GraphLink link)
        {
            await CreateParentChain(link.Source);
            await CreateParentChain(link.Target);
            string id = link.Source.Id.ToString() + "->" + link.Target.Id.ToString();            
            if (!createdLinks.Contains(id))
            {
                createdLinks.Add(id);
                GraphCategory category = link.Categories.FirstOrDefault();
                await pipe.SendReceiveAsync(new CreateLinkMessage(link.Source.Id.ToString(), link.Target.Id.ToString(), link.Label, link.Index, category?.Id));
            }

            await pipe.SendReceiveAsync(new NavigateLinkMessage(link.Source.Id.ToString(), link.Target.Id.ToString()));
        }
        
        /// <summary>
        /// Write an additional message to the client
        /// </summary>
        /// <param name="format">String to be formatted using string.format</param>
        /// <param name="args">The arguments</param>
        public async Task WriteMessage(string format, params object[] args)
        {
            string msg = string.Format(format, args);
            await pipe.SendReceiveAsync(new ClearTextMessage(msg));
        }

        /// <summary>
        /// Close the pipe
        /// </summary>
        public void Close()
        {
            if (this.source != null)
            {
                this.source.Cancel();
            }
            using (pipe)
            {
                pipe = null;
            }
        }
    }
}
