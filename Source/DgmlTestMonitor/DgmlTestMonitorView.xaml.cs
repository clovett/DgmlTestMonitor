using LovettSoftware.DgmlTestModeling;
using Microsoft.VisualStudio.GraphModel;
using Microsoft.VisualStudio.GraphModel.Styles;
using Microsoft.VisualStudio.Progression;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DgmlTestMonitor
{
    /// <summary>
    /// Interaction logic for DgmlTestMonitorView.xaml
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable")]
    public partial class DgmlTestMonitorView : UserControl
    {
        GraphStateReader reader;
        string fileName;
        GraphControl graphControl;
        IServiceProvider serviceProvider;
        SelectionTracker tracker;
        Dispatcher dispatcher;
        Queue<Message> _messages = new Queue<Message>();
        DispatcherTimer messageTimer;
        ObservableCollection<LogViewModel> logItems;
        bool disableAutoScroll;

        const string LoadingModel = "Loading Model:";
        const string EnteringState = "Entering state";
        const string NavigatingLink = "Navigating link";


        public DgmlTestMonitorView()
        {
            InitializeComponent();

            dispatcher = this.Dispatcher;
            LogView.SelectionChanged += LogView_SelectionChanged;

            // initialize buttons to disabled
            OnActiveWindowChanged(this, new ActiveWindowChangedEventArgs(null));

            logItems = new ObservableCollection<LogViewModel>();
            LogView.ItemsSource = logItems;

            reader = new GraphStateReader();
            reader.MessageReceived += OnMessageArrived;
            var nowait = reader.Start();
        }

        void OnBreakPoint(GraphObject trigger)
        {
            if (trigger is GraphNode)
            {
                // output window?
                // AppendLog("Hit breakpoint on node");
            }
            else if (trigger is GraphLink)
            {
                // output window?
                //AppendLog("Hit breakpoint on link");
            }
            UpdatePlayPauseButtonState();
        }

        internal void OnClose()
        {
            using (reader)
            {
                reader.MessageReceived -= OnMessageArrived;
                reader = null;
            }
            serviceProvider = null;
            tracker.ActiveWindowChanged -= OnActiveWindowChanged;
            tracker = null;
        }

        public void OnInitialized(System.IServiceProvider provider)
        {
            serviceProvider = provider;
            tracker = provider.GetService(typeof(SelectionTracker)) as SelectionTracker;
            if (tracker != null)
            {
                tracker.ActiveWindowChanged += OnActiveWindowChanged;
                OnActiveWindowChanged(this, new ActiveWindowChangedEventArgs(tracker.ActiveWindow));
            }
        }

        void OnActiveWindowChanged(object sender, ActiveWindowChangedEventArgs e)
        {
            bool isEnabled = (e.Window != null);
            ClearBreakpointButton.IsEnabled = isEnabled;
            SetBreakpointButton.IsEnabled = isEnabled;
            RemoveAllBreakpointsButton.IsEnabled = isEnabled;
        }
        
        void LogView_SelectionChanged(object sender, RoutedEventArgs e)
        {
            bool extend = false;

            // sync the selection to the new selection
            foreach (LogViewModel item in LogView.SelectedItems)
            {
                OnSelectItem(item, extend);
                extend = true;
            }
        }

        string GetNodeLabelOrId(GraphNode node)
        {
            if (!string.IsNullOrEmpty(node.Label))
            {
                return TransformLabel(node.Label);
            }
            return node.Id.ToString();
        }

        void OnMessageArrived(object sender, Message msg)
        {
            lock (_messages)
            {
                _messages.Enqueue(msg);
            }
            StartProcessMessages();
        }

        object msgLock = new object();

        void StartProcessMessages()
        {
            if (messageTimer == null)
            {
                Dispatcher.BeginInvoke(new System.Action(() => {
                    // throttle to update UI 30 times a second, any more locks up the VS UI
                    lock (msgLock)
                    {
                        if (messageTimer == null)
                        {
                            messageTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(30), DispatcherPriority.ApplicationIdle, OnProcessMessages, this.Dispatcher);
                        }
                        messageTimer.Start();
                    }
                }));
            }
        }

        List<Message> GetNextBatch()
        {
            List<Message> list = new List<Message>();
            lock (_messages)
            {
                while (_messages.Count > 0)
                {
                    list.Add(_messages.Dequeue());
                }
            }
            return list;
        }

        void OnProcessMessages(object sender, EventArgs args)
        {
            if (graphLoaded && !layoutReady)
            {
                return;
            }
            // catch up with the background queue in one sweep, this batch update helps the UI keep up
            List<Message> batch = GetNextBatch();
            int i = 0;
            
            if (graphControl == null || graphControl.Graph == null)
            {
                graphControl = null;

                // in this case we can't do anything until a graph is loaded
                foreach (Message msg in batch)
                {
                    i++;
                    if (msg is LoadGraphMessage)
                    {
                        OnGraphLoaded((LoadGraphMessage)msg);
                        break;
                    }
                }
            }

            if (graphControl == null)
            {
                return;
            }

            if (batch.Count == 0)
            {
                noMessageCount++;
                if (noMessageCount > 10)
                {
                    lock (msgLock)
                    {
                        messageTimer.Stop();
                        messageTimer.Tick -= OnProcessMessages;
                        messageTimer = null;
                    }
                }
            }
            noMessageCount = 0;

            Graph graph = graphControl.Graph;
            if (graph == null)
            {
                // graph has closed.
                graphControl = null;
                fileName = null;
                return;
            }
            if (batch.Count == 0)
            {
                return;
            }
            bool autoScroll = this.AutoScroll;
            int end = logItems.Count;

            using (var update = graph.BeginUpdate(new object(), "Sync", UndoOption.Disable))
            {
                this.disableAutoScroll = true;

                for (; i < batch.Count; i++)
                {
                    Message msg = batch[i];
                    if (msg is NavigateLinkMessage)
                    {
                        OnGraphLinkNavigated((NavigateLinkMessage)msg);
                    }
                    else if (msg is NavigateNodeMessage)
                    {
                        OnGraphNodeNavigated((NavigateNodeMessage)msg);
                    }
                    else if (msg is LoadGraphMessage)
                    {
                        OnGraphLoaded((LoadGraphMessage)msg);
                    }
                    else if (msg is ClearTextMessage)
                    {
                        ClearTextMessage text = (ClearTextMessage)msg;
                        // AppendLog(text.Message); // outputwindow ?
                    }
                }

                // Update selection
                List<GraphObject> selection = new List<GraphObject>();
                for (i = end; i < logItems.Count; i++)
                {
                    LogViewModel model = logItems[i];
                    selection.Add(model.Object);
                }

                // show this whole batch so that if the log is really fast we still get to see what is happening.
                AnimateNewSelection(selection, false, true);

                update.Complete();
            }

            if (autoScroll)
            {
                // scroll to end, but don't update the graph selection when log view selection changes.
                LogView.SelectionChanged -= LogView_SelectionChanged;
                ScrollToEnd();
                LogView.SelectionChanged += LogView_SelectionChanged;
            }

            this.disableAutoScroll = false;
        }

        int noMessageCount;

        private void OnGraphLoaded(LoadGraphMessage msg)
        {
            logItems.Clear();
            LoadGraph(msg.Path);
        }
        
        private void OnGraphNodeNavigated(NavigateNodeMessage msg)
        {
            if (!string.IsNullOrEmpty(msg.NodeId))
            {
                string id = msg.NodeId;
                string label = msg.NodeLabel;
                Graph graph = graphControl.Graph;
                GraphNode node = graph.Nodes.GetOrCreate(id, label, null);
                if (breakpoints.Contains(node))
                {
                    reader.Pause();
                    OnBreakPoint(node);
                }

                AppendLog(new NodeViewModel() { Object = node, Label = GetNodeLabelOrId(node), Id=node.Id.ToString() });
            }
        }

        void OnGraphLinkNavigated(NavigateLinkMessage msg)
        {
            if (!string.IsNullOrEmpty(msg.SourceNodeId) && !string.IsNullOrEmpty(msg.TargetNodeId))
            {
                string sid = msg.SourceNodeId;
                string slabel = msg.SourceNodeLabel;
                string tid = msg.TargetNodeId;
                string tlabel = msg.TargetNodeLabel;
                string label = msg.Label;
                int index = msg.Index;
                Graph graph = graphControl.Graph;
                GraphNode source = graph.Nodes.GetOrCreate(sid, slabel, null);
                GraphNode target = graph.Nodes.GetOrCreate(tid, tlabel, null);
                GraphLink link = graph.Links.GetOrCreate(source.Id, target.Id, index);
                if (!string.IsNullOrEmpty(label) && link.Label != label)
                {
                    link.Label = label;
                }
                if (breakpoints.Contains(link))
                {
                    reader.Pause();
                    OnBreakPoint(link);
                }

                AppendLog(new LinkViewModel()
                {
                    Object = link,
                    Source = GetNodeLabelOrId(link.Source),
                    SourceId = link.Source.Id.ToString(),
                    Target = GetNodeLabelOrId(link.Target),
                    TargetId = link.Target.Id.ToString()
                });

            }

        }


        string UnescapeQuotes(string s)
        {
            return s.Replace("&apos;", "'");
        }


        bool AutoScroll
        {
            get
            {
                if (disableAutoScroll) return false;
                return (LogView.SelectedIndex >= logItems.Count - 2);
            }
        }

        void AppendLog(LogViewModel msg)
        {
            logItems.Add(msg);

            if (AutoScroll)
            {
                ScrollToEnd();
            }
        }

        void ScrollToEnd()
        {
            if (logItems.Count > 0)
            {
                if (logItems.Count > 0)
                {
                    int last = logItems.Count - 1;
                    var item = logItems[last];
                    LogView.SelectedIndex = last;
                    LogView.ScrollIntoView(item);
                }
            }
        }

        void OnSelectItem(LogViewModel item, bool extendSelection)
        {
            if (item is NodeViewModel)
            {
                OnNavigatingNode((NodeViewModel)item, extendSelection);
            }
            else if (item is LinkViewModel)
            {
                OnNavigatingLink((LinkViewModel)item, extendSelection);
            }
        }
        
        private bool OpenGraph()
        {
            if (string.IsNullOrEmpty(fileName)) 
            {
                return false;
            }
            
            GraphControl control = ActiveGraphControl;
            if (control == null)
            {
                graphControl = null;
            }

            if (control != graphControl || graphControl == null)
            {
                LoadGraph(fileName);
            }
            
            return true;
        }

        private void AnimateNewSelection(List<GraphObject> items, bool extendSelection, bool activateState)
        {
            var selection = graphControl.Selection;
            using (var update = selection.BeginUpdate())
            {
                bool wasFocused = LogView.IsKeyboardFocusWithin;
                var graph = graphControl.Graph;

                
                if (!extendSelection)
                {
                    graphControl.Selection.Clear();
                }

                foreach (var item in items)
                { 
                    selection.Add(item);
                    //graphControl.ScrollTo(link);
                    if (activateState && item is GraphNode)
                    {
                        //graphControl.NavigateTo(node, false);
                        ActivateState((GraphNode)item);
                    }
                }
            }
        }

        const string ActiveCategoryId = "Active";
        const string DebugCategoryId = "Debug";
        GraphCategory cat;
        Graph catGraph;

        private GraphCategory GetActiveCategory()
        {
            Graph graph = graphControl.Graph;
            if (catGraph != graph || cat == null)
            {
                cat = graph.DocumentSchema.FindCategory(ActiveCategoryId);
                if (cat == null)
                {
                    cat = graph.DocumentSchema.Categories.AddNewCategory(ActiveCategoryId);
                }
                EnsureNodeStyle(graph, "Active", "True", "HasCategory('Active')", "Background", "#FF5A9747");
                catGraph = graph;
            }
            return cat;
        }

        private void ActivateState(GraphNode node)
        {
            Debug.WriteLine("Activating node: " + node.Label);

            // we also want to show the active state in a given group
            GraphGroup parent = node.ParentGroups.FirstOrDefault();
            if (parent != null)
            {
                GraphCategory category = GetActiveCategory();
                foreach (GraphNode child in parent.ChildNodes)
                {
                    if (child.HasCategory(category) && child != node)
                    {
                        child.RemoveCategory(category);
                        // bugbug: UI is not updating when we remove categories, so we make this edit as well.
                        
                    }
                }
                if (!node.HasCategory(category))
                {
                    node.AddCategory(category);
                }
            }
        }

        private void OnNavigatingLink(LinkViewModel item, bool extendSelection)
        {
            if (!OpenGraph())
            {
                return;
            }

            Graph graph = graphControl.Graph;
            GraphLink link = item.Object as GraphLink;
            if (link != null)
            {
                List<GraphObject> items = new List<GraphObject>();
                items.Add(link);
                AnimateNewSelection(items, extendSelection, false);
            }

        }

        string TransformLabel(string label)
        {
            return (""+label).Replace("\r", "").Replace("\n", ".");
        }

        private void OnNavigatingNode(NodeViewModel item, bool extendSelection)
        {
            if (!OpenGraph())
            {
                return;
            }
            string label = item.Label;
            var graph = graphControl.Graph;

            GraphNode node = item.Object as GraphNode;
            if (node != null)
            {
                List<GraphObject> items = new List<GraphObject>();
                items.Add(node);
                AnimateNewSelection(items, extendSelection, false);
            }
        }

        private Graph LoadGraph(string path)
        {
            try
            {
                fileName = path;
                EnvDTE.DTE dte = serviceProvider.GetService(typeof(EnvDTE._DTE)) as EnvDTE.DTE;
                EnvDTE.Window window = dte.ItemOperations.OpenFile(path);
                IVSGraphControl automation = window.Object as IVSGraphControl;
                WindowPane pane = automation.Window as WindowPane;
                graphControl = pane.Content as GraphControl;
                graphLoaded = true;
                layoutReady = false;
                graphControl.LayoutUpdated += OnGraphLayoutUpdated;
                return graphControl.Graph;
            }
            catch 
            {
                // pipe contained weird file name then, so ignore it...
            }
            return null;
        }

        bool graphLoaded;
        bool layoutReady;

        private void OnGraphLayoutUpdated(object sender, EventArgs e)
        {
            layoutReady = true;
        }

        private void OnTogglePause(object sender, RoutedEventArgs e)
        {
            if (reader.IsPaused)
            {
                // output window?
                //AppendLog("Resumed");
                reader.Resume();
            }
            else
            {
                // output window?
                // AppendLog("Paused..."); 
                reader.Pause();
            }
            UpdatePlayPauseButtonState();
        }

        private void UpdatePlayPauseButtonState()
        {
            ToolbarButton button = PauseButton;
            if (reader.IsPaused)
            {
                button.ToolTip = "Resume exection of the model";
                button.IconUri = "Resources/Play.png";
            }
            else
            {
                button.ToolTip = "Pause exection of the model";
                button.IconUri = "Resources/Pause.png";
            }
        }

        private void OnSetBreakpoint(object sender, RoutedEventArgs e)
        {
            GraphControl control = ActiveGraphControl;
            if (control == null)
            {
                return;
            }
            Graph graph = control.Graph;
            using (var update = graph.BeginUpdate(new object(), "Add Breakpoints", UndoOption.Add))
            {
                foreach (GraphObject g in control.Selection)
                {
                    this.AddBreakpoint(g);
                }
                update.Complete();
            }

            // add it.
            using (var update = graph.BeginUpdate(new object(), "Add Breakpoint Style", UndoOption.Add))
            {
                EnsureNodeStyle(graph, "Breakpoint", "True", "HasCategory('Breakpoint')", "Icon", 
                            "pack://application:,,,/Microsoft.VisualStudio.Progression.GraphControl;component/Icons/kpi_red_cat1_large.png");
                EnsureLinkStyle(graph, null, null, "HasCategory('Breakpoint')", "Icon",
                           "pack://application:,,,/Microsoft.VisualStudio.Progression.GraphControl;component/Icons/kpi_red_cat1_large.png");

                update.Complete();
            }
        }

        private void EnsureNodeStyle(Graph graph, string groupLabel, string valueLabel, string expression, string property, string value)
        {

            // make sure the graph contains the Style to display this 
            /*
            <Style TargetType="Node" GroupLabel="Breakpoint" ValueLabel="True">
              <Condition Expression="HasCategory('Breakpoint')" />
              <Setter Property="Icon" Value="pack://application:,,,/Microsoft.VisualStudio.Progression.GraphControl;component/Icons/kpi_red_cat1_large.png" />
            </Style>
             */
            foreach (var s in graph.Styles)
            {
                if (s.TargetType == typeof(GraphNode))
                {
                    foreach (var c in s.Conditions)
                    {
                        if (c.Expression == expression)
                        {
                            return; // got it.
                        }
                    }
                }
            }

            GraphConditionalStyle style = new GraphConditionalStyle(graph);
            style.GroupLabel = groupLabel;
            style.ValueLabel = valueLabel;
            style.TargetType = typeof(GraphNode);
            GraphCondition condition = new GraphCondition(style);
            condition.Expression = expression;
            style.Conditions.Add(condition);
            GraphSetter setter = new GraphSetter(style, property);
            setter.Value = value;
            style.Setters.Add(setter);
            graph.Styles.Insert(0, style);
        }

        private void EnsureLinkStyle(Graph graph, string groupLabel, string valueLabel, string expression, string property, string value)
        {
            // make sure the graph contains the Style to display this 
            /*
            <Style TargetType="Link" GroupLabel="Breakpoint" ValueLabel="True">
              <Condition Expression="HasCategory('Breakpoint')" />
              <Setter Property="Icon" Value="pack://application:,,,/Microsoft.VisualStudio.Progression.GraphControl;component/Icons/kpi_red_cat1_large.png" />
            </Style>
             */
            foreach (var s in graph.Styles)
            {
                if (s.TargetType == typeof(GraphLink))
                {
                    foreach (var c in s.Conditions)
                    {
                        if (c.Expression == expression)
                        {
                            return; // got it.
                        }
                    }
                }
            }

            GraphConditionalStyle style = new GraphConditionalStyle(graph);
            style.GroupLabel = groupLabel;
            style.ValueLabel = valueLabel;
            style.TargetType = typeof(GraphLink);
            GraphCondition condition = new GraphCondition(style);
            condition.Expression = expression;
            style.Conditions.Add(condition);
            GraphSetter setter = new GraphSetter(style, property);
            setter.Value = value;
            style.Setters.Add(setter);
            graph.Styles.Insert(0, style);
        }

        private void OnClearBreakpoint(object sender, RoutedEventArgs e)
        {
            GraphControl control = ActiveGraphControl;
            if (control == null)
            {
                return;
            }
            Graph graph = control.Graph;
            using (var update = graph.BeginUpdate(new object(), "Clear Breakpoints", UndoOption.Add))
            {
                foreach (GraphObject g in control.Selection)
                {
                    this.RemoveBreakpoint(g);
                }
                update.Complete();
            }

        }

        private void OnRemoveAllBreakpoints(object sender, RoutedEventArgs e)
        {
            GraphControl control = ActiveGraphControl;
            if (control == null)
            {
                return;
            }
            Graph graph = control.Graph;
            using (var update = graph.BeginUpdate(new object(), "Clear Breakpoints", UndoOption.Add))
            {
                this.ClearBreakpoints();
                update.Complete();
            }
        }

        private GraphControl ActiveGraphControl
        {
            get
            {                
                if (tracker == null) return null;
                IGraphDocumentWindowPane pane = tracker.ActiveWindow;
                if (pane == null) return null;

                WindowPane windowPane = pane as WindowPane;
                if (windowPane == null) return null;

                return (GraphControl)windowPane.Content;
            }
        }

        #region Breakpoints

        HashSet<GraphObject> breakpoints = new HashSet<GraphObject>();


        /// <summary>
        /// Add a breakpoint on the given node
        /// </summary>
        /// <param name="nodeOrLink">The node or link to stop on</param>
        public void AddBreakpoint(GraphObject nodeOrLink)
        {
            nodeOrLink.AddCategory(DgmlTestModelSchema.BreakpointCategory);
            breakpoints.Add(nodeOrLink);
        }

        /// <summary>
        /// Remove the breakpoint on the given node
        /// </summary>
        /// <param name="nodeOrLink">The node or link</param>
        public void RemoveBreakpoint(GraphObject nodeOrLink)
        {
            if (nodeOrLink.HasCategory(DgmlTestModelSchema.BreakpointCategory))
            {
                nodeOrLink.RemoveCategory(DgmlTestModelSchema.BreakpointCategory);
            }
            breakpoints.Remove(nodeOrLink);
        }

        /// <summary>
        /// Clear all breakpoints and resume running.
        /// </summary>
        public void ClearBreakpoints()
        {
            foreach (GraphObject g in breakpoints)
            {
                if (g.HasCategory(DgmlTestModelSchema.BreakpointCategory))
                {
                    g.RemoveCategory(DgmlTestModelSchema.BreakpointCategory);
                }
            }
            breakpoints.Clear();

            if (reader.IsPaused)
            {
                reader.Resume();
            }
        }

        #endregion 

    }
}