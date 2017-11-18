using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZeroMQ;

namespace stock_notify
{
	class ZmqSubscriber
	{
		private readonly ZContext _zmqContext;
		private readonly ZSocket _zmqSocket;
		private readonly Thread _workerThread;
		private readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
		private readonly object _locker = new object();
		private readonly Queue<string> _queue = new Queue<string>();

		public ZmqSubscriber(string endPoint)
		{
			_zmqContext = new ZContext();
			_zmqSocket = new ZSocket(_zmqContext, ZSocketType.SUB);
			_zmqSocket.Connect(endPoint);
			_zmqSocket.SubscribeAll();

			_workerThread = new Thread(ReceiveData);
			_workerThread.Start();
		}

		public string[] GetMessages()
		{
			lock (_locker)
			{
				var messages = _queue.ToArray();
				_queue.Clear();
				return messages;
			}
		}

		public void Stop()
		{
			_stopEvent.Set();
			_workerThread.Join();
		}

		private void ReceiveData()
		{
			try
			{
				while (!_stopEvent.WaitOne(0))
				{
					var message = _zmqSocket.ReceiveMessage();
					if (string.IsNullOrEmpty(message.PopString()))
						continue;

					lock (_locker)
						_queue.Enqueue(message.PopString());
				}
			}
			finally
			{
				_zmqSocket.Dispose();
				_zmqContext.Dispose();
			}
		}
	}
}
