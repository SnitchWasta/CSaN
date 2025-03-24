using System.Net.Sockets;
using System.Net.WebSockets;
using System.Net.Security;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace chat_ptp
{
    public partial class Form1 : Form
    {
        List<Sock_et> connections = new List<Sock_et>();
        IPAddress MyClientIP;
        bool isHas = false;
        string nickname = "";
        int local_ports = 6000;
        bool correct_start = false;
        List<string> history = new List<string>();
        public Form1()
        {
            InitializeComponent();
            Text = "Придумайте себе имя";
            chat_text_box.KeyDown += Chat_flow_KeyDown; ;
            FormClosing += Form1_FormClosing;
        }



        /// Form functions and events
        private void Chat_flow_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) { sent_message(); }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            string hh_mm = now.Hours + ":" + (now.Minutes > 10 ? now.Minutes : "0" + now.Minutes);
            string mess = $"[{hh_mm}] {nickname} отключился";
            foreach (var t in connections)
            {
                t.Send(mess);
                try
                {
                    t.Socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"{MyClientIP}: {ex.Message}");
                }
            }
        }

        private async void Start()
        {
            connections = new List<Sock_et>();
            string myIp = textBox2.Text;
            try
            {
                MyClientIP = IPAddress.Parse(myIp);
                try
                {
                    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    s.Bind(new IPEndPoint(MyClientIP, 10000));
                    s.Close();
                    s.Dispose();
                }
                catch
                {
                    MessageBox.Show("Этот IP адрес не доступен для использования");
                    return;
                }
            }
            catch {
                MessageBox.Show("IP адресс указан неверно");
            }
            Invoke((MethodInvoker)delegate
            {
                nickname = textBox1.Text;
                textBox1.Enabled = false;
                textBox2.Enabled = false;
                name_button.Enabled = false;
                chat_text_box.Enabled = true;
                send_button.Enabled = true;
                chat_flow.Enabled = true;
                this.Text = $"My IP: {MyClientIP}, my nickname: {nickname}";
            });
            await Task.Factory.StartNew(() => { UdpListener(); });
            await Task.Factory.StartNew(() => { SendBroadcast(); });
            await Task.Factory.StartNew(() => { Tcp_Listener(); });

        }
        public void add_to_chat(string message, bool isMine = false, bool isAdding = true)
        {
            if (isAdding) history.Add(message);
            Invoke((MethodInvoker)async delegate
            {
                Label label = new Label();
                label.Text = message;
                label.Font = new Font("Times New Roman", 20);
                label.AutoSize = true;
                label.Margin = new Padding(5);
                label.MinimumSize = new Size(chat_flow.Width - 25, 40);
                label.MaximumSize = new Size(chat_flow.Width - 25, 1000);
                label.BackColor = (!isMine ? Color.AliceBlue : Color.LightGreen);
                chat_flow.Controls.Add(label);
                int newY = chat_flow.VerticalScroll.Maximum;
                chat_flow.AutoScrollPosition = new Point(0, newY);
            });
        }
        private void change_history(List<string> strings)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            string hh_mm = now.Hours + ":" + (now.Minutes > 10 ? now.Minutes : "0" + now.Minutes);
            Invoke((MethodInvoker)async delegate
            {
                chat_flow.Controls.Clear();
                history = new List<string>();
                for (int i = 1; i < strings.Count - 2; i++)
                {
                    add_to_chat(strings[i], false, false);
                    history.Add(strings[i]);
                }

                add_to_chat($"[{hh_mm}] {nickname} подключён", true);
            });
        }

        private void sent_message()
        {
            if (string.IsNullOrEmpty(chat_text_box.Text)) return;

            string mess = $"[{DateTime.Now:HH:mm}] {nickname}: {chat_text_box.Text}";
            chat_text_box.Clear();
            add_to_chat(mess, true);

            lock (connections)
            {
                foreach (var connection in connections.ToArray())
                {
                    try
                    {
                        connection.Send(mess);
                    }
                    catch
                    {
                        connections.Remove(connection);
                    }
                }
            }
        }
        /// end of Form functions





        /// buttons
        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(() => { Start(); });
        }
        private void send_button_Click(object sender, EventArgs e)
        {
            sent_message();
        }
        /// End of buttons




        /// UDP ///
        private async void UdpListener()
        {
            while (true)
            {
                string message = await ReceiveUpdMessage();
                string ip = message.Split(';')[0];

                if (ip == MyClientIP.ToString() ||
                    connections.Any(c => c.Socket.Connected &&
                        ((IPEndPoint)c.Socket.RemoteEndPoint).Address.ToString() == ip))
                    continue;

                try
                {
                    var clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clientSocket.Bind(new IPEndPoint(MyClientIP, local_ports++));
                    await Task.Run(() => clientSocket.Connect(ip, 5545));
                    
                    var sock = new Sock_et();
                    sock.MessageReceived += (msg) => add_to_chat(msg);
                    sock.Start(clientSocket, history);

                    lock (connections)
                    {
                        connections.Add(sock);
                    }
                }
                catch { }
            }
        }
        public async void SendBroadcast()
        {
            IPAddress broadcastAddress = IPAddress.Parse("255.255.255.255");
            int broadcastPort = 5555;
            try
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(new IPEndPoint(MyClientIP, broadcastPort));
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                string message = $"{MyClientIP};{nickname}";
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                IPEndPoint endPoint = new IPEndPoint(broadcastAddress, broadcastPort);
                await socket.SendToAsync(buffer, endPoint);
            }
            catch (SocketException ex)
            {
                MessageBox.Show($"Socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
        public async Task<string> ReceiveUpdMessage()
        {
            using var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpSocket.Bind(new IPEndPoint(IPAddress.Any, 5555));
            byte[] data = new byte[256];
            EndPoint remoteIp = new IPEndPoint(IPAddress.Parse("255.255.255.255"), 0);
            var result = await udpSocket.ReceiveFromAsync(data, remoteIp);
            udpSocket.Dispose();
            var message = Encoding.UTF8.GetString(data, 0, result.ReceivedBytes);
            return (message);
        }
        /// End of UDP ///
        
        /// Tcp_Listener
        private async void Tcp_Listener()
        {
            var listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(MyClientIP, 5545));
            listener.Listen(100);
            TimeSpan now = DateTime.Now.TimeOfDay;
            string hh_mm = now.Hours + ":" + (now.Minutes > 10? now.Minutes: "0" + now.Minutes);
            add_to_chat($"[{hh_mm}] {nickname} подключён", true);
            while (true)
            {
                var clientSocket = await Task.Factory.FromAsync(
                    listener.BeginAccept,
                    listener.EndAccept,
                    null
                );
                var sock = new Sock_et();
                sock.MessageReceived += (msg) => add_to_chat(msg);
                sock.HistoryReceived += (strs) => change_history(strs);
                sock.Start(clientSocket, history);
                sock.Send($"[{hh_mm}] {nickname} подключён");
                if (!isHas)
                {
                    isHas = true;
                    sock.Send("history");
                }
                lock (connections)
                {
                    connections.Add(sock);
                }
            }
        }
        /// End of Tcp_Listener


        public class Sock_et
        {
            List<string> history;
            public Socket Socket { get; private set; }
            public event Action <string> MessageReceived;

            public event Action<List<string>> HistoryReceived;
            private byte[] _buffer = new byte[1024];

            public Sock_et()
            {
                Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            }

            public void Start(Socket acceptedSocket, List<string> history)
            {
                this.history = history;
                Socket = acceptedSocket;
                BeginReceive();
            }

            private void BeginReceive()
            {
                try
                {
                    Socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, ReceiveCallback, null);
                }
                catch (Exception ex)
                {
                    HandleDisconnect();
                }
            }

            private void ReceiveCallback(IAsyncResult ar)
            {
                try
                {
                    int bytesRead = Socket.EndReceive(ar);
                    
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                        if (message.Split('~')[0] == "hist")
                        {
                            HistoryReceived(message.Split('~').ToList());
                            BeginReceive();
                        }
                        else
                        {
                            //MessageBox.Show(message);
                            if (message == "history")
                            {
                                string sendr = "hist~";
                                foreach (string line in history)
                                {
                                    sendr += line + "~";
                                }
                                sendr += "hist";
                                Send(sendr);
                                BeginReceive();
                            }
                            else
                            {
                                MessageReceived?.Invoke(message);
                                BeginReceive();
                            }
                        }
                    }
                    else
                    {
                        HandleDisconnect();
                    }
                }
                catch
                {
                    HandleDisconnect();
                }
            }

            public void Send(string message)
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                try
                {
                    Socket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, null);
                }
                catch { }
            }

            private void SendCallback(IAsyncResult ar)
            {
                try
                {
                    Socket.EndSend(ar);
                }
                catch
                {
                    HandleDisconnect();
                }
            }

            private void HandleDisconnect()
            {
                try
                {
                    Socket.Shutdown(SocketShutdown.Receive);
                }
                catch { }
            }
        }
    }


    }


// -sntch