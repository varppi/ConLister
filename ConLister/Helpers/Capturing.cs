using Avalonia.Input;
using Avalonia.Threading;
using ConLister.Models;
using ConLister.ViewModels;
using PacketDotNet;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConLister.Helpers
{

    internal class Capturing
    {
        public delegate void ConnectionHandler(Connection connection);
        public delegate void RefreshHandler();
        public event ConnectionHandler? OnConnection;
        public event RefreshHandler? OnRefresh;

        private Refresher? _refresher;
        private ICaptureDevice? _captureDevice;
        private readonly Dictionary<int, ICaptureDevice> _captureDevices = new();

        /// <summary>
        /// Returns a list of interfaces/capture devices available.
        /// </summary>
        /// <returns></returns>
        public CaptureDeviceList? IndexCaptureDevices()
        {
            var captureDevices = CaptureDeviceList.Instance;
            int index = 0;
            foreach (var captureDevice in captureDevices) {
                _captureDevices[index] = captureDevice;
                index++;
            };
            return captureDevices;
        }

        /// <summary>
        /// Starts to listen for connetions.
        /// </summary>
        /// <param name="index"></param>
        public void StartCapture(int index)
        {
            if (_captureDevice is not null) StopCapture();
            _captureDevice  = _captureDevices[index];
            _captureDevice.Open();
            _captureDevice.OnPacketArrival += 
                new SharpPcap.PacketArrivalEventHandler(onNewPacket);
            _captureDevice.StartCapture();
            _refresher = new();
            _refresher.OnRefresh += () => { if (this.OnRefresh is not null) this.OnRefresh(); };
            _refresher?.Start();
        }

        /// <summary>
        /// Stops listening for connections.
        /// </summary>
        public void StopCapture()
        {
            _refresher?.Stop();
            _captureDevice?.StopCapture();
            _captureDevice?.Close();
        }

        /// <summary>
        /// New packet handler parses the packet and sends it to the OnConnection handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void onNewPacket(object sender, PacketCapture e) {
            var rawPacket = e.GetPacket();
            var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
            var tcp = packet.Extract<PacketDotNet.TcpPacket>();
            if (tcp == null) return;
            var ipSect = (PacketDotNet.IPPacket)tcp.ParentPacket;
            Dispatcher.UIThread.Invoke(() => 
                OnConnection?.Invoke(
                    new Connection
                    {
                        SourceIP = ipSect.SourceAddress.ToString(),
                        DestionationIP = ipSect.DestinationAddress.ToString(),
                        SourcePort = tcp.SourcePort,
                        DestinationPort = tcp.DestinationPort,
                        Seen = DateTime.Now,
                    }
                )
            );
        }
    }

    internal class Refresher
    {
        public delegate void RefreshHandler();
        public event RefreshHandler? OnRefresh;
        private Thread? _workerThread;
        private CancellationTokenSource? _workerCancellationToken;

        /// <summary>
        /// Refreshes the connections every 250ms to make sure "secs ago" updates. 
        /// </summary>
        private void worker()
        {
            while (true)
            {
                try{_workerCancellationToken?.Token.ThrowIfCancellationRequested();}
                catch { return; }
                Dispatcher.UIThread.Invoke(() => OnRefresh?.Invoke());
                Thread.Sleep(250);
            }
        }

        /// <summary>
        /// Starts the refresh worker.
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void Start()
        {
            if (_workerThread is not null) 
                throw new Exception("previous worker wasn't stopped");
            _workerCancellationToken = new CancellationTokenSource();
            _workerThread = new Thread(worker);
            _workerThread.Start();
        }

        /// <summary>
        /// Stops the refresh worker.
        /// </summary>
        public void Stop()
            => _workerCancellationToken?.Cancel();
    }
}
