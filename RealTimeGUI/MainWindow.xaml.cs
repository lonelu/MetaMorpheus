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
using System.Threading;
using System.Globalization;
using System.Collections.ObjectModel;
using TaskLayer;
using System.IO;
using EngineLayer;
using Proteomics;




namespace RealTimeGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
       
        private LogWatcher logWatcher;

        public DataReceiver DataReceiver { get; set; }
        public RealTimeTask RealTimeTask { get; set; }
        //public Notifications Notifications { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            RealTimeTask = new RealTimeTask();

            DataReceiver = new DataReceiver();

            //Notifications = new Notifications();
            //DataContext = Notifications;

            //DataContext = DataReceiver.Notifications;


            DataReceiver.DataReceiverNotificationEventHandler += UpdateTbNotification;

            // Create a LogFileWatcher to display the log and bind the log textbox to it
            logWatcher = new LogWatcher();
            logWatcher.Updated += logWatcher_Updated;

        }

        private void UpdateTbNotification(object sender, NotificationEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => UpdateTbNotification(sender, e)));
            }
            else
            {
                RtbNotifications.AppendText(e.Notification);
            }    
        }

        private void BtnConnection_Click(object sender, RoutedEventArgs e)
        {
            //log.Debug("Start log");
            //Notifications.Notification = "AA";

            //DataReceiver.Notifications.Notification += "AA";

            //DataReceiver.TestLog();
            DataReceiver.InstrumentAccess = Connection.GetFirstInstrument();
            DataReceiver.ScanContainer = DataReceiver.InstrumentAccess.GetMsScanContainer(0);
            RtbNotifications.AppendText(DataReceiver.InstrumentAccess.InstrumentName);
        }

        private void BtnDisConnection_Click(object sender, RoutedEventArgs e)
        {
            //Put the following code into a function 
            DataReceiver.ScanContainer = null;
            DataReceiver.InstrumentAccess = null;
        }

        private void BtnRealTimeData_Click(object sender, RoutedEventArgs e)
        {
            DataReceiver.ReceiveData();
            Thread.CurrentThread.Join(DataReceiver.RTParameters.TimeScale);
            DataReceiver.StopReceiveData();
            //DataReceiver.TestLog();

            //Notifications.Notification = "AB";

        }

        public void logWatcher_Updated(object sender, EventArgs e)
        {
            UpdateLogTextbox(logWatcher.LogContent);
        }

        public void UpdateLogTextbox(string value)
        {
            // Check whether invoke is required and then invoke as necessary
            if (!Dispatcher.CheckAccess())
            {

                Dispatcher.BeginInvoke(new Action(() => UpdateLogTextbox(value)));
                return;
            }

            // Set the textbox value
            //RtbNotifications.AppendText(value);

            //Notifications.Notification = value;

        }

        private void UpdateParametersFromTask()
        {
            TbTimeScale.Text = DataReceiver.RTParameters.TimeScale.ToString(CultureInfo.InvariantCulture);
        }

        private void UpdateParametersFromGUI()
        {
            DataReceiver.RTParameters.TimeScale = int.Parse(TbTimeScale.Text);
        }

        private void GuiWarnHandler(object sender, StringEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => GuiWarnHandler(sender, e)));
            }
            else
            {
                RtbNotifications.AppendText(e.S);
                RtbNotifications.AppendText(Environment.NewLine);
            }
        }
    }
}
