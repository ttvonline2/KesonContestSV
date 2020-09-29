using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

namespace KesonContestSV
{
    public class SocketT2h
    {
        public Socket _Socket { get; set; }
        public string _Name { get; set; }
        public SocketT2h(Socket socket)
        {
            this._Socket = socket;
        }
    }
    public partial class MainWindow : Window
    {
        #region var
        string[] st_Result = new string[20];
        int int_gk = 8;
        int[] int_ttGK = new int[20];
        private const int PORT = 197;
        IPAddress address = IPAddress.Parse("10.12.20.25");
        String filePath;

        private byte[] _buffer = new byte[1024];
        int int_curGk = 0;
        public List<SocketT2h> __ClientSockets { get; set; }
        List<string> _names = new List<string>();
        private Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        #endregion

        public MainWindow()
        {
            InitializeComponent();
            __ClientSockets = new List<SocketT2h>();
        }


        #region SetupServer
        void SetupServer()
        {
            string addr = tb_address.Text.ToString();
            address = IPAddress.Parse(addr);
            _serverSocket.Bind(new IPEndPoint(address, PORT));
            _serverSocket.Listen(1);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void AcceptCallback(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);

            __ClientSockets.Add(new SocketT2h(socket));
                
            var dispatcher = this.Dispatcher;
            // Or use this.Dispatcher if this method is in a window class.

            dispatcher.BeginInvoke(new Action(() =>
            {
                lbox_Client.Items.Add(socket.RemoteEndPoint.ToString());
                int_curGk++;

            }));

            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);

        }

        int Search_Index_Adress(string address_now)
        {
            for (int i = 0; i < lbox_Client.Items.Count; i++)
            {
                if (address_now == lbox_Client.Items[i].ToString())
                {
                    return i;
                }
            }
            return -1;

        }
        private void ReceiveCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            if (socket.Connected)
            {
                int received;
                try
                {
                    received = socket.EndReceive(AR);
                }
                catch (Exception)
                {
                    // client đóng kết nối
                    for (int i = 0; i < __ClientSockets.Count; i++)
                    {
                        string t1 = __ClientSockets[i]._Socket.RemoteEndPoint.ToString();
                        string t2 = socket.RemoteEndPoint.ToString();
                        if (t1 == t2)
                        {

                            var dispatcher = this.Dispatcher;
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                int_curGk--;
                                lbox_Client.Items.RemoveAt(i);
                                
                            }));

                            update_GK(i);
                            __ClientSockets.RemoveAt(i);

                        }
                    }
                    // xóa trong list
                    return;
                }
                if (received != 0)
                {
                    byte[] dataBuf = new byte[received];
                    Array.Copy(_buffer, dataBuf, received);
                    string text2 = Encoding.ASCII.GetString(dataBuf);
                    string text = text2.Substring(2);

                    var dispatcher = this.Dispatcher;
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        string temp = socket.RemoteEndPoint.ToString() + "/" + text;
                        tb_receive.Text = temp;
                        XuLyText(text);
                    }));

                    string reponse = string.Empty;

                    for (int i = 0; i < __ClientSockets.Count; i++)
                    {
                        if (socket.RemoteEndPoint.ToString().Equals(__ClientSockets[i]._Socket.RemoteEndPoint.ToString()))
                        {
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                //lbStatus2.Content = ("\n" + __ClientSockets[i]._Name + ": " + text);
                            }));

                        }
                    }
                    if (text == "bye")
                    {
                        return;
                    }
                    reponse = text;
                    //Sendata(socket, reponse);
                }
                else
                {
                    for (int i = 0; i < __ClientSockets.Count; i++)
                    {
                        if (__ClientSockets[i]._Socket.RemoteEndPoint.ToString().Equals(socket.RemoteEndPoint.ToString()))
                        {


                            var dispatcher = this.Dispatcher;
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                //lbStatus.Content = "Số client đang kết nối: " + __ClientSockets.Count.ToString();
                                lbox_Client.Items.RemoveAt(i - 1);
                                int_curGk--;
                                update_GK(i);
                            }));
                            __ClientSockets.RemoveAt(i);
                        }
                    }
                }
            }
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        void Sendata(Socket socket, string noidung)
        {
            byte[] data = Encoding.ASCII.GetBytes(noidung);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        private void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetupServer();
        }
        #endregion
        void XuLyText(string text)
        {
            string st_case = text.Substring(0, 3);
            if(st_case == "*cn")
            {
                lb_status.Content = text.Substring(3, 1) + " - Connected!";
                int_ttGK[int_curGk-1] = Convert.ToInt16(text.Substring(3, 1));
                string a = "";
                for (int i = 0; i < int_gk; i++)
                {
                    a += int_ttGK[i] + "\t";
                }
                lb_status.Content = a;
            }
        }
        void update_GK(int vt)
        {
            string a = "";
            for(int i = (vt); i < int_gk; i++)
            {
                int_ttGK[i] = int_ttGK[i + 1];
            }
            for(int i = 0; i < int_gk; i++)
            {
                a += int_ttGK[i] + "\t";
            }
            var dispatcher = this.Dispatcher;
            dispatcher.BeginInvoke(new Action(() =>
            {
                lb_status.Content = a;
            }));

        }
    }
}
