using Avalonia.Controls;
using Avalonia.Interactivity;
using ConLister.Helpers;
using ConLister.Models;
using ConLister.ViewModels;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConLister.Views
{
    public partial class MainWindow : Window
    {
        private const int _captureDeviceMaxLength = 40;
        private Capturing _capturing;
        private bool _captureStarted;

        public MainWindow()
        {
            this.DataContext = new MainWindowViewModel();
            InitializeComponent();
            initializeApp();
        }

        /// <summary>
        /// Hydrates available interfaces/capture devices and sets up handlers.
        /// </summary>
        private void initializeApp()
        {
            if (this.DataContext is null) return;

            _capturing = new();
            _capturing.OnConnection += connectionHandler;
            _capturing.OnRefresh += refreshHandler;
            var captureDevices = _capturing.IndexCaptureDevices()?.ToList();
            foreach(var captureDevice in captureDevices ?? [])
            {
                var newComboItem = new ComboBoxItem{};
                newComboItem.Content = string.Join("", 
                    captureDevice.Description.Select((ch,i) 
                        => i > _captureDeviceMaxLength 
                            ? (i < _captureDeviceMaxLength+2 ? "..." : "") 
                            : $"{ch}"
                        ));
                AvailableCaptureDevices.Items.Add(newComboItem);
            }
        }

        /// <summary>
        /// Refreshes the UI with up to date connections.
        /// </summary>
        private void refreshHandler()
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;
            viewModel.RefreshConnections();
        }

        /// <summary>
        /// Handles new connections being found.
        /// </summary>
        /// <param name="connection"></param>
        private void connectionHandler(Connection connection)
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;

            viewModel.AddConnection(connection);
        }

        /// <summary>
        /// Starts or stops listening for connections depending what the status is.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void click_StartStopListening(object sender, RoutedEventArgs e)
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;

            if (_captureStarted)
            {
                _capturing.StopCapture();
                viewModel.CaptureButtonText = "Start Capture";
                _captureStarted = false;
                return;
            }

            viewModel.ErrorMessage = "";
            if (viewModel.SelectedCaptureDevice == -1)
            {
                viewModel.ErrorMessage = "You must select a valid capture interface";
                return;
            }

             viewModel.ClearConnections();
            _captureStarted = true;
            viewModel.CaptureButtonText = "Stop Capture";
            _capturing.StartCapture(viewModel.SelectedCaptureDevice);
        }

        /// <summary>
        /// Copies all destination IPs to your clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void click_CopyIPOnly(object sender, RoutedEventArgs e)
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;

            StringBuilder finalString = new StringBuilder();
            Dictionary<string, bool> IPs = new();
            foreach(KeyValuePair<string, Connection> connection in viewModel.ConnectionLog)
                IPs[connection.Value.DestionationIP] = true;

            if (Clipboard is null) return;
            await Clipboard.SetTextAsync(string.Join('\n', IPs.Keys.ToList()));
        }

        /// <summary>
        /// Copies destination IP:PORT combinations to the clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void click_CopyIPPort(object sender, RoutedEventArgs e)
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;

            StringBuilder finalString = new StringBuilder();
            Dictionary<string, bool> IPPort = new();
            foreach (KeyValuePair<string, Connection> connection in viewModel.ConnectionLog)
                IPPort[$"{connection.Value.DestionationIP}:{connection.Value.DestinationPort}"] = true;

            if (Clipboard is null) return;
            await Clipboard.SetTextAsync(string.Join('\n', IPPort.Keys.ToList()));
        }

        /// <summary>
        /// Copies all IP:PORT combinations to your clipboard.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void click_CopyAllIPPort(object sender, RoutedEventArgs e)
        {
            var viewModel = (DataContext as MainWindowViewModel);
            if (viewModel is null) return;

            StringBuilder finalString = new StringBuilder();
            Dictionary<string, bool> IPPort = new();
            foreach (KeyValuePair<string, Connection> connection in viewModel.ConnectionLog)
            {
                IPPort[$"{connection.Value.DestionationIP}:{connection.Value.DestinationPort}"] = true;
                IPPort[$"{connection.Value.SourceIP}:{connection.Value.SourcePort}"] = true;
            }

            if (Clipboard is null) return;
            await Clipboard.SetTextAsync(string.Join('\n', IPPort.Keys.ToList()));
        }
    }
}