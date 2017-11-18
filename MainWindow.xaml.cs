using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using NetMQ;
using NetMQ.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace stock_notify
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		NotifyIcon trayIcon;
		System.Windows.Forms.ContextMenu trayMenu;

		//ZmqSubscriber zmqSubscriber = new ZmqSubscriber("tcp://127.0.0.1:5556");
		BackgroundWorker zmqWorker = new BackgroundWorker();

		public MainWindow()
		{
			InitializeComponent();

			createNotifyIcon();

			SnapToScreep();

			//
			Subscribe();

			//
#if DEBUG
			Tick t = new Tick();
			t.code = "000001.SH";
			t.count = 2;
			t.lastVol = 4.5f;
			t.time = DateTime.Now.ToString();
			Ticks.Add(t);

			t = new Tick();
			t.code = "000725.SZ";
			t.count = 3;
			t.lastVol = 14.5f;
			t.time = DateTime.Now.ToString();
			Ticks.Add(t);
#endif
		}

		private void Subscribe()
		{
			zmqWorker.WorkerSupportsCancellation = true;
			zmqWorker.WorkerReportsProgress = true;
			zmqWorker.DoWork += Zmq_DoWork;
			zmqWorker.ProgressChanged += Zmq_ProgressChanged;

			zmqWorker.RunWorkerAsync(Properties.Settings.Default.mqserver);
			//zmqWorker.RunWorkerAsync("tcp://127.0.0.1:5556");

			//using (var context = new ZContext())
			//using (var subscriber = new ZSocket(context, ZSocketType.SUB))
			//{
			//	subscriber.Connect("tcp://127.0.0.1:5556");
			//	subscriber.SubscribeAll();

			//	while (true)
			//	{
			//		using (var frame = subscriber.ReceiveFrame())
			//		{
			//			string msg = frame.ReadString();
			//		}
			//		//using (var frame = subscriber.ReceiveMessage())
			//		//{
			//		//	string msg = frame.PopString();
			//		//}
			//	}
			//}
		}


		private ObservableCollection<Tick> m_ticks = new ObservableCollection<Tick>();
		public ObservableCollection<Tick> Ticks
		{
			get
			{
				return m_ticks;
			}
		}

		private Dictionary<string, Tick> m_dict_ticks = new Dictionary<string, Tick>();
		private void Zmq_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			string msg = (string)e.UserState;
			Console.WriteLine(msg);

			var sa = msg.Split(',');

			var c = sa[1];
			var t = sa[0];
			var vol = float.Parse(sa[2]);

			Tick tick;
			if (m_dict_ticks.ContainsKey(c))
			{
				tick = m_dict_ticks[c];
				Console.WriteLine("update tick: " + c);
			}
			else
			{
				tick = new Tick();
				tick.code = c;
				m_dict_ticks.Add(c, tick);

				Ticks.Add(tick);

				//
				Console.WriteLine("new tick: " + c);
			}

			tick.count++;
			tick.time = t;
			tick.lastVol = vol;

			//
			sortAndSyncTicks();

			try
			{
				Show();
			}
			catch (Exception)
			{
			}
		}

		private void sortAndSyncTicks()
		{

		}

		private void Zmq_DoWork(object sender, DoWorkEventArgs e)
		{
			BackgroundWorker worker = sender as BackgroundWorker;

			using (var sub = new SubscriberSocket((string)e.Argument))
			{
				sub.SubscribeToAnyTopic();

				while (true)
				{
					var msg = sub.ReceiveFrameString();

					// "tick {str(ts)},{c},{s['rt_last_vol']}"
					worker.ReportProgress(1, msg.Substring(5));
				}
			}
		}

		private void SnapToScreep()
		{
			Screen screen = Screen.FromPoint(new System.Drawing.Point((int)Left, (int)Top));

			Left = screen.WorkingArea.Right - Width;
			Top = screen.WorkingArea.Bottom - Height;
		}

		private void createNotifyIcon()
		{
			// Create a simple tray menu with only one item.
			trayMenu = new System.Windows.Forms.ContextMenu();
			trayMenu.MenuItems.Add("&Show", OnMenuShow);
			trayMenu.MenuItems.Add("-");
			trayMenu.MenuItems.Add("E&xit", OnMenuExit);

			// Create a tray icon. In this example we use a
			// standard system icon for simplicity, but you
			// can of course use your own custom icon too.
			trayIcon = new NotifyIcon();
			trayIcon.Text = "MyTrayApp";
			trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
			trayIcon.DoubleClick += OnMenuShow;

			// Add menu to tray icon and show it.
			trayIcon.ContextMenu = trayMenu;
			trayIcon.Visible = true;
		}

		private void OnMenuShow(object sender, EventArgs e)
		{
			SnapToScreep();
			Show();
		}

		private void OnMenuExit(object sender, EventArgs e)
		{
			Console.WriteLine("on exit");
			Close();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			zmqWorker.CancelAsync();
			trayIcon.Dispose();
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			StackFrame[] frames = new StackTrace().GetFrames();
			bool wasCodeClosed = frames.FirstOrDefault(x => x.GetMethod() == typeof(Window).GetMethod("Close")) != null;
			if (wasCodeClosed)
			{
				// Closed with this.Close()
			}
			else
			{
				// Closed some other way.
				e.Cancel = true;
				Hide();
			}
		}

		private void showStock(string code)
		{
			var prc = Process.GetProcessesByName(Properties.Settings.Default.process);
			if (prc.Length > 0)
			{
				SetForegroundWindow(prc[0].MainWindowHandle);
				ShowWindow(prc[0].MainWindowHandle, SW_MAXIMIZE);

				//SendKeys.SendWait("0");
				//SendKeys.SendWait("0");
				//SendKeys.SendWait("0");
				//SendKeys.SendWait("0");
				//SendKeys.SendWait("0");
				//SendKeys.SendWait("1");
				SendKeys.SendWait(code);
				Thread.Sleep(1 * 1000);
				SendKeys.SendWait("{ENTER}");
				//SendKeys.Flush();
			}
		}
		[DllImport("user32.dll")]
		private static extern bool SetForegroundWindow(IntPtr hWnd);

		private const int SW_MAXIMIZE = 3;
		private const int SW_MINIMIZE = 6;
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		private void listTicks_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//get the selectitem
			ListBoxItem b = e.Source as ListBoxItem;

			// get the select Binding Item 
			var item = b.Content as Tick;

			//
			if (item != null)
				showStock(item.code.Split(',')[0]);
			else
				Console.WriteLine("null item");
		}

		private void btnClean_Click(object sender, RoutedEventArgs e)
		{
			if (System.Windows.MessageBox.Show("清除列表？", "确认", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
			{
				m_dict_ticks.Clear();
				Ticks.Clear();
			}
		}
	}

	public class Tick
	{
		public string code { get; set; }
		public string time { get; set; }
		public float lastVol { get; set; }

		public int count { get; set; }
	}
}
