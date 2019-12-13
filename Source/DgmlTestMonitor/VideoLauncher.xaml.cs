using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;
using System.Runtime.InteropServices;

namespace DgmlTestMonitor
{
    /// <summary>
    /// Interaction logic for VideoLauncher.xaml
    /// </summary>
    public partial class VideoLauncher : UserControl
    {
        public VideoLauncher()
        {
            InitializeComponent();

        }

        public event EventHandler HideVideo;

        private void OnPlay(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            if (!string.IsNullOrEmpty(VideoUrl))
            {
                OpenUrl(VideoUrl);
            }
        }

        public static void OpenUrl(string url)
        {
            const int SW_SHOWNORMAL = 1;
            int rc = ShellExecute(IntPtr.Zero, "open", url, null, "", SW_SHOWNORMAL);
        }


        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1060:MovePInvokesToNativeMethodsClass"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Interoperability", "CA1401:PInvokesShouldNotBeVisible"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "2"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "4"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "3"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), DllImport("Shell32.dll", EntryPoint = "ShellExecuteA",
            SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true,
            CallingConvention = CallingConvention.StdCall)]
        private static extern int ShellExecute(IntPtr handle, string verb, string file,
            string args, string dir, int show);

        public string VideoUrl
        {
            get { return (string)GetValue(VideoUrlProperty); }
            set { SetValue(VideoUrlProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoUrl.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoUrlProperty =
            DependencyProperty.Register("VideoUrl", typeof(string), typeof(VideoLauncher), new PropertyMetadata(null));

        

        public string VideoDescription
        {
            get { return (string)GetValue(VideoDescriptionProperty); }
            set { SetValue(VideoDescriptionProperty, value); }
        }

        // Using a DependencyProperty as the backing store for VideoDescription.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty VideoDescriptionProperty =
            DependencyProperty.Register("VideoDescription", typeof(string), typeof(VideoLauncher), new PropertyMetadata(null));

        

        public string LinkText
        {
            get { return (string)GetValue(LinkTextProperty); }
            set { SetValue(LinkTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkTextProperty =
            DependencyProperty.Register("LinkText", typeof(string), typeof(VideoLauncher), new PropertyMetadata(null));



        public string LinkTip
        {
            get { return (string)GetValue(LinkTipProperty); }
            set { SetValue(LinkTipProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkTip.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkTipProperty =
            DependencyProperty.Register("LinkTip", typeof(string), typeof(VideoLauncher), new PropertyMetadata(null));

        private void OnHideVideo(object sender, RoutedEventArgs e)
        {
            if (HideVideo != null)
            {
                HideVideo(this, EventArgs.Empty);
            }
        }
    }
}
