using Avalonia.Controls;
using Avalonia.Threading;
using ConLister.Helpers;
using ConLister.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text;

namespace ConLister.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
    {
        private int selectedCaptureDevice = -1;
        public int SelectedCaptureDevice { get => selectedCaptureDevice;
            set {
                this.selectedCaptureDevice = value;
                OnPropertyChanged(nameof(SelectedCaptureDevice));
            } 
        }

        private string errorMessage = string.Empty;
        public string ErrorMessage { get => errorMessage == "" ? "" : $"ERROR: {errorMessage}";
            set { 
                this.errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        private string captureButtonText = "Start Capture";
        public string CaptureButtonText
        {
            get => captureButtonText;
            set
            {
                this.captureButtonText = value;
                OnPropertyChanged(nameof(CaptureButtonText));
            }
        }

        public Dictionary<string, Connection> ConnectionLog = new();
        public string ConnectionLogFormatted
        {
            get => formatConnectionLog();
            private set { }
        }

        /// <summary>
        /// Adds a new connection to the internal dict.
        /// </summary>
        /// <param name="connection"></param>
        public void AddConnection(Connection connection)
        {
            ConnectionLog[connection.DestionationIP] = connection;
            OnPropertyChanged(nameof(ConnectionLogFormatted));
        }

        /// <summary>
        /// Removes all connections from the internal dict.
        /// </summary>
        public void ClearConnections()
        {
            ConnectionLog.Clear();
            OnPropertyChanged(ConnectionLogFormatted);
        }

        /// <summary>
        /// Tells Avalonia that the property has changed so it would update the new value.
        /// </summary>
        public void RefreshConnections() {
            OnPropertyChanged(ConnectionLogFormatted);
        }

        /// <summary>
        /// Returns a nice string version of the connection log.
        /// </summary>
        /// <returns></returns>
        private string formatConnectionLog()
        {
            // Sorts connections from most recent to oldest
            Dictionary<int, List<Connection>> timeRelConnections = new();
            var now = DateTime.Now;
            foreach (KeyValuePair<string, Connection> pair in ConnectionLog)
            {
                var key = now.Subtract(pair.Value.Seen).Seconds;
                if (!timeRelConnections.ContainsKey(key))
                    timeRelConnections[key] = new();
                timeRelConnections[key].Add(pair.Value);
            }
            var sorted = timeRelConnections.Keys.ToList();
            sorted.Sort();
            ////

            StringBuilder finalString = new();
            foreach (var key in sorted) 
                foreach (Connection con in timeRelConnections[key])
                    finalString.AppendLine($"{con.SourceIP}:{con.SourcePort} --> {con.DestionationIP}:{con.DestinationPort} Seen: {DateTime.Now.Subtract(con.Seen).Seconds} secs ago");

            return finalString.ToString();
        }

        #region TwoWayBinding
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion
    }
}
