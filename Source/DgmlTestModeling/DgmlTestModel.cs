using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Schemas;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Threading;

namespace LovettSoftware.DgmlTestModeling
{
    /// <summary>
    /// This is a DGML schema which defines GraphCategory and GraphProperty objects that are 
    /// used by the DgmlTestModel to find various important things in a DGML Test Document.
    /// </summary>
    public class DgmlTestModelSchema
    {
        /// <summary>
        /// Instance of the GraphSchema
        /// </summary>
        public static GraphSchema Schema;

        static DgmlTestModelSchema() 
        { 
            Schema = new GraphSchema("DgmlTestModelSchema");
            EntryPointCategory = Schema.Categories.AddNewCategory("EntryPoint");
            SingletonCategory = Schema.Categories.AddNewCategory("Singleton");
            BreakpointCategory = Schema.Categories.AddNewCategory("Breakpoint");

            PriorityProperty = Schema.Properties.AddNewProperty("Priority", typeof(double));
            ChildPriorityProperty = Schema.Properties.AddNewProperty("ChildPriority", typeof(double));
            DisabledProperty = Schema.Properties.AddNewProperty("Disabled", typeof(bool));
        }

        /// <summary>
        /// Nodes with this category define an entry point for the model either
        /// at the root level, or inside a group.
        /// </summary>
        public static GraphCategory EntryPointCategory;

        /// <summary>
        /// Nodes with this category are executed mutually exclusively.
        /// </summary>
        public static GraphCategory SingletonCategory;

        /// <summary>
        /// Nodes with this category will cause the DGML Test Monitor to pause on this node.
        /// </summary>
        public static GraphCategory BreakpointCategory;

        /// <summary>
        /// This property can be used on a link to define a higher or lower priority relative
        /// to other links.  Normally integer values are used like 1 through 10, but it is
        /// typed as a double just in case you need that.
        /// </summary>
        public static GraphProperty PriorityProperty;

        /// <summary>
        /// This property can be used on a group to define the priority for entering the children
        /// of the group as opposed to navigating away from the group.  This would be the
        /// same as adding a Priority to the Contains links, but it makes it easier to
        /// do in the DGML editor where you can't see the containment links.
        /// </summary>
        public static GraphProperty ChildPriorityProperty;

        /// <summary>
        /// Nodes with Disabled=true are ignored during model execution so this is
        /// a convenient way to skip them temporarily.
        /// </summary>
        public static GraphProperty DisabledProperty;
    }

    /// <summary>
    /// This class loads a given DGML test model and executes it.  The execution state is 
    /// writen to a named pipe so that the DGML Test Monitor VSIX package can watch and
    /// animate the progress through the model.
    /// </summary>
    public class DgmlTestModel
    {
        Graph model;
        GraphNode currentState;
        Random random;
        object target;
        TextWriter log;
        int statesExecuted;
        bool stop;
        GraphStateWriter writer;
        GraphNode currentGroup;
        GraphNode lastSingleton; 

        /// <summary>
        /// Construct a new DgmlTestModel for execution against the given target object.
        /// When a node is chosen in the model a method with the same name is looked for on
        /// the target object and if found that method is called.  When a link is being
        /// considered a boolean property with the same name as the link label is looked up
        /// and if found it is called.  If false is returned the link is not considered.
        /// </summary>
        /// <param name="target">The target object that implements the node state methods and link predicates</param>
        /// <param name="log">The log to write to</param>
        /// <param name="r">The random number generator to use</param>
        public DgmlTestModel(object target, TextWriter log, Random r)
        {
            this.random = r;
            this.target = target;
            this.log = log;
            writer = new GraphStateWriter(log);
            writer.Connect().Wait();
        }

        /// <summary>
        /// Load the DGML document at the given path.  This document contains nodes
        /// and links decorated with categories and properties defined in the 
        /// DgmlTestModelSchema.  You can also define a set of styles to colorize the
        /// special categories.  
        /// </summary>
        /// <param name="path"></param>
        public void Load(string path) 
        {
            writer.LoadGraph(path).Wait();
            model = Graph.Load(path, DgmlTestModelSchema.Schema);
        }

        /// <summary>
        /// Execute the model until the given predicate returns false.
        /// </summary>
        /// <param name="until">The predicate that determines when the model should stop executing.</param>
        /// <param name="sleep">You can add a delay between each state to slow down the execution, this is handy for debugging.</param>
        public void Run(Predicate<DgmlTestModel> until, Int32 sleep) 
        {
            while (!stop)
            {
                if (currentState != null)
                { 
                    currentState = FindTransition(currentState);
                }
                if (currentState == null)
                {
                    // top level nodes only (ones that have no incoming containment links)
                    currentState = FindRootEntryPoint();
                    currentGroup = null;
                    lastSingleton = null;
                }

                try
                {
                    ExecuteState(currentState);
                }
                catch (Exception e)
                {
                    // see if we can recover
                    if (!HandleUnexpectedError(e))
                    {
                        writer.Close();
                        throw;
                    }
                    else
                    {
                        // try current state again.
                        ExecuteState(currentState);
                    }
                }

                if (until(this))
                {
                    writer.Close();
                    return;
                }

                if (sleep > 0)
                {
                    Thread.Sleep(sleep);
                }
            }
        }

        private bool HandleUnexpectedError(Exception e)
        {         
            MethodInfo mi = target.GetType().GetMethod("HandleException", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, new Type[] { typeof(Exception) }, null);
            if (mi == null)
            {
                writer.WriteMessage("No error handler found").Wait();
                return false;
            }

            object obj = (mi.IsStatic ? null : target);
            object result = mi.Invoke(obj, new object[] { e });

            if (result is bool)
            {
                return (bool)result;
            }
            return false;
        }

        private GraphNode FindRootEntryPoint()
        {
            return FindEntryPoint(from n in model.Nodes
                                          where                                                
                                           !n.GetValue<bool>(DgmlTestModelSchema.DisabledProperty) &&
                                           !((from p in n.IncomingLinks where p.HasCategory(GraphCommonSchema.Contains) select p).Any())
                                          select n);
        }

        /// <summary>
        /// Call this method to stop the model.
        /// </summary>
        public void Stop()
        {
            stop = true;
        }

        /// <summary>
        /// Returns the number of states executed so far.
        /// </summary>
        public int StatesExecuted
        {
            get { return statesExecuted; }
            set { statesExecuted = value; }
        }

        private GraphNode FindTransition(GraphNode currentState)
        {
            TransitionChoices choices = new TransitionChoices(random, executeCounts);

            double? childPriority = null;
            if (currentState.IsGroup && currentState.HasValue(DgmlTestModelSchema.ChildPriorityProperty))
            {
                childPriority = currentState.GetValue<double>(DgmlTestModelSchema.ChildPriorityProperty);
            }            

            foreach (GraphLink choice in currentState.OutgoingLinks)
            {
                if (CanNavigate(choice))
                {
                    double w = 1;
                    if (choice.HasValue(DgmlTestModelSchema.PriorityProperty))
                    {
                        w = choice.GetValue<double>(DgmlTestModelSchema.PriorityProperty);
                    }
                    else if (childPriority.HasValue)
                    {
                        w = childPriority.Value;
                    }
                    if (w < 1) w = 1;

                    choices.Add(choice, w);
                }
            }

            // If we have no choices, move back up to parent
            if (choices.Count == 0)
            {                
                foreach (GraphLink containment in currentState.IncomingLinks)
                {
                    if (containment.HasCategory(GraphCommonSchema.Contains))
                    {
                        // then walk back up to parent group and try an exit transition.
                        writer.NavigateToNode(containment.Source).Wait();
                        currentState = containment.Source;
                        return FindTransition(currentState);
                    }
                }

                // if it has no parents, then try and re-enter at the root level.
                return null;
            }

            // GetExecuteCount

            // pick an entry point at random.
            GraphLink link = choices.GetRandomChoice();
            writer.NavigateLink(link).Wait();
            GraphNode newState = link.Target;

            GraphNode group = null;
            if (newState.IsGroup)
            {
                group = newState;
            }
            else
            {
                foreach (GraphNode node in  newState.FindAncestors()) 
                {
                    if (node.IsGroup) 
                    {
                        group = node;
                        break;
                    }
                }
            }

            if (currentGroup != group)
            {
                lastSingleton = null;
            }
            currentGroup = group;            
            
            if (newState.HasCategory(DgmlTestModelSchema.SingletonCategory))
            {
                lastSingleton = newState;
            }

            return newState;
        }

        /// <summary>
        /// This class manages the list of links to choose from and picks the best one based on
        /// priority and whether or not we've already gone down that path yet or not.
        /// </summary>
        class TransitionChoices
        {
            Random random;
            int count;
            Dictionary<double, List<GraphLink>> linksByPriority = new Dictionary<double,List<GraphLink>>();
            Dictionary<GraphNode, int> history;

            public TransitionChoices(Random r, Dictionary<GraphNode, int> history)
            {
                random = r;
                this.history = history;
            }

            public void Add(GraphLink link, double priority)
            {
                List<GraphLink> list;
                if (!linksByPriority.TryGetValue(priority, out list))
                {
                    list = new List<GraphLink>();
                    linksByPriority[priority] = list;
                }
                list.Add(link);
                count++;
            }

            public int Count { get { return count; } }


            public GraphLink GetRandomChoice()
            {
                List<GraphLink> choices = new List<GraphLink>();

                foreach (KeyValuePair<double, List<GraphLink>> pair in linksByPriority)
                {
                    var priority = pair.Key;                  
                    var list = pair.Value;
                    foreach (GraphLink link in list)
                    {
                        if (priority < 1) priority = 1;
                        for (int i = 0; i < priority; i++)
                        {
                            choices.Add(link);
                        }
                    }

                    list.Sort(new Comparison<GraphLink>((a, b) =>
                    {
                        int aExecuted = 0;
                        history.TryGetValue(a.Target, out aExecuted);
                        int bExecuted = 0;
                        history.TryGetValue(b.Target, out bExecuted);
                        return aExecuted - bExecuted;
                    }));

                    int min = 0;
                    int max = 0;
                    foreach (GraphLink link in list)
                    {
                        int e = 0;
                        history.TryGetValue(link.Target, out e);
                        min = Math.Min(min, e);
                        max = Math.Max(max, e);
                    }

                    if (min == 0 && max > 0)
                    {
                        // we have some links that haven't been executed yet, so give 'em a go!
                        List<GraphLink> notyet = new List<GraphLink>();
                        foreach (GraphLink link in list)
                        {
                            int e = 0;
                            history.TryGetValue(link.Target, out e);
                            if (e == 0)
                            {
                                notyet.Add(link);
                                return notyet[random.Next(0, notyet.Count)];
                            }
                        }
                    }
                }

                if (choices.Count == 0)
                {
                    return null;
                }
                return choices[random.Next(0, choices.Count)];
            }

        }

        private string GetLinkDebugLabel(GraphLink link) 
        {
            return GetLabelOrId(link.Source) + "->" + GetLabelOrId(link.Target);
        }

        private string GetLabelOrId(GraphNode node)
        {
            string label = node.Label;
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }
            return node.Id.ToString();
        }

        private bool CanNavigate(GraphLink choice)
        {
            if (choice.Target.GetValue<bool>(DgmlTestModelSchema.DisabledProperty))
            {
                return false;
            }
            if (choice.HasCategory("Comment"))
            {
                // ignore comment nodes.
                return false;
            }
            if (choice.HasCategory(GraphCommonSchema.Contains))
            {
                // implement the "singleton" rule for children of a group.  The "singleton" rule means
                // only one of a given set of singleton choices can be executed and it can only be executed once.
                if (lastSingleton != null &&
                    IsParent(choice.Source, lastSingleton) && 
                    choice.Target.HasCategory(DgmlTestModelSchema.SingletonCategory))
                {
                    return false;
                }
                
                // if we are navigating into a group, then we need to pick an entry point.
                return choice.Target.HasCategory(DgmlTestModelSchema.EntryPointCategory);                                
            }

            string label = choice.Label;
            if (string.IsNullOrEmpty(label))
            {
                return true;
            }

            // invoke boolean property getter
            PropertyInfo pi = target.GetType().GetProperty(label, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, typeof(bool), new Type[0], null);
            if (pi == null)
            {
                writer.WriteMessage("No implementation found for link '{0}'", label).Wait();
                return false;
            }

            bool value = (bool)pi.GetValue(target, null);

            writer.WriteMessage("? Can Navigate link '{0}' : {1}", label, value).Wait();
            return value;
        }

        private bool IsParent(GraphNode parent, GraphNode node)
        {
            foreach (GraphLink link in node.IncomingLinks)
            {
                if (link.HasCategory(GraphCommonSchema.Contains) && link.Source == parent)
                {
                    return true;
                }
            }
            return false;
        }

        private GraphNode GetParent(GraphNode node)
        {           
            foreach (GraphLink link in node.IncomingLinks)
            {
                if (link.HasCategory(GraphCommonSchema.Contains))
                {
                    return link.Source;
                }
            }
            return null;
        }

        private Dictionary<GraphNode, int> executeCounts = new Dictionary<GraphNode, int>();

        private void IncrementCount(GraphNode state)
        {
            int count = GetExecuteCount(state);
            count++;
            executeCounts[currentState] = count;
        }

        private int GetExecuteCount(GraphNode state)
        {
            int count = 0;
            executeCounts.TryGetValue(currentState, out count);
            return count;
        }

        private void ExecuteState(GraphNode currentState)
        {
            // invoke method with name equal to the Label of this node.
            string label = GetLabelOrId(currentState);

            IncrementCount(currentState);

            writer.NavigateToNode(currentState).Wait();

            MethodInfo mi = target.GetType().GetMethod(label, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, new Type[0], null);
            if (mi == null)
            {
                writer.WriteMessage("Error: No implementation found for state '{0}'", label).Wait();
                return;
            }

            if (mi.IsStatic)
            {
                mi.Invoke(null, null);
            }
            else
            {
                mi.Invoke(target, null);
            }
            
            statesExecuted++;
        }

        private GraphNode FindEntryPoint(IEnumerable<GraphNode> nodes)
        {
            List<GraphNode> choices = new List<GraphNode>(from n in nodes where n.HasCategory(DgmlTestModelSchema.EntryPointCategory) select n);
            // pick an entry point at random.
            if (choices.Count == 0) 
            {
                throw new Exception("No choices remaining, so model is terminating");
            }

            return choices[random.Next(0, choices.Count)];
        }       
    }

}
