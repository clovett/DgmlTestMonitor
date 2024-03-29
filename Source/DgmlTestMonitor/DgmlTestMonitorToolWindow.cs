﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Windows;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Progression;

namespace DgmlTestMonitor
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    ///
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane, 
    /// usually implemented by the package implementer.
    ///
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its 
    /// implementation of the IVsUIElementPane interface.
    /// </summary>
    [Guid("0b5d9891-3590-4dfb-94c2-8fb5e92550f6")]
    public class DgmlTestMonitorToolWindow : ToolWindowPane
    {
        SelectionTracker tracker;

        /// <summary>
        /// Standard constructor for the tool window.
        /// </summary>
        public DgmlTestMonitorToolWindow() :
            base(null)
        {
            // Set the window title reading it from the resources.
            this.Caption = Resources.ToolWindowTitle;
            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            this.BitmapResourceID = 301;
            this.BitmapIndex = 1;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on 
            // the object returned by the Content property.
            var view = new DgmlTestMonitorView();
            base.Content = view;
        }

        protected override object GetService(Type serviceType)
        {
            if (serviceType == typeof(SelectionTracker))
            {
                return tracker;
            }
            return base.GetService(serviceType);
        }

        protected override void Initialize()
        {
            base.Initialize();

            var sp = (System.IServiceProvider)this;
            tracker = new SelectionTracker(sp);
            DgmlTestMonitorView view = (DgmlTestMonitorView)this.Content;
            view.OnInitialized(sp);
        }

        protected override void OnClose()
        {
            DgmlTestMonitorView view = (DgmlTestMonitorView)this.Content;
            view.OnClose();
            base.OnClose();
            using (tracker)
            {
            }
        }

    }
}
