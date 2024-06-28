using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace RPS_Client
{
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread receiveThread;
        private bool gameEnded = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string ipAddress = IpAddressTextBox.Text;
            int port;
            if (!int.TryParse(PortTextBox.Text, out port))
            {
                MessageBox.Show("Invalid port number.");
                return;
            }

            try
            {
                client = new TcpClient(ipAddress, port);
                stream = client.GetStream();
                receiveThread = new Thread(new ThreadStart(ReceiveData));
                receiveThread.Start();
                MessageBox.Show($"Connected to server {ipAddress}:{port}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Connection error: " + ex.Message);
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                stream.Close();
                client.Close();
                receiveThread.Abort(); // Note: Thread.Abort is not recommended; consider using a different approach for thread termination.
                MessageBox.Show("Disconnected from server");
            }
        }

        private void MoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null && !gameEnded)
            {
                string move = (sender as Button).Content.ToString();
                byte[] moveBytes = Encoding.UTF8.GetBytes(move);
                stream.Write(moveBytes, 0, moveBytes.Length);
            }
            else
            {
                MessageBox.Show("Connect to the server first or game has ended.");
            }
        }

        private void EndGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                gameEnded = true;
                string endMessage = "END_GAME";
                byte[] endBytes = Encoding.UTF8.GetBytes(endMessage);
                stream.Write(endBytes, 0, endBytes.Length);
                stream.Close();
                client.Close();
                receiveThread.Abort(); // Terminate receiving thread
                MessageBox.Show("Game ended and disconnected from server.");
            }
        }

        private void ReceiveData()
        {
            while (true)
            {
                try
                {
                    byte[] buffer = new byte[256];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                        break;

                    string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    if (serverMessage.StartsWith("RESULT:"))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ResultTextBlock.Text += serverMessage.Substring(7) + Environment.NewLine;
                        });
                    }
                    else if (serverMessage.StartsWith("GAME_END"))
                    {
                        gameEnded = true;
                        Dispatcher.Invoke(() =>
                        {
                            ResultTextBlock.Text += "Game ended by server." + Environment.NewLine;
                        });
                        break;
                    }
                }
                catch (Exception)
                {
                    Dispatcher.Invoke(() =>
                    {
                        ResultTextBlock.Text = "Disconnected from server";
                    });
                    break;
                }
            }
        }
    }
}
