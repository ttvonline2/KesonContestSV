using System;
using System.Collections.Generic;
using System.IO;
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
        int int_removeID;
        int int_gk = 9;
        int[] int_ttGK = new int[20];
        private const int PORT = 197;
        IPAddress address = IPAddress.Parse("10.12.20.25");
        int[,] int_AllResult = new int[10, 4];
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
            if(addr == "")
            {
                addr = "10.12.20.25";
            }
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
                            int_removeID = i;
                            var dispatcher = this.Dispatcher;
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                int_curGk--;
                                lbox_Client.Items.RemoveAt(int_removeID);
                                Icon_Online(int_ttGK);
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

                            int_removeID = i;
                            var dispatcher = this.Dispatcher;
                            dispatcher.BeginInvoke(new Action(() =>
                            {
                                //lbStatus.Content = "Số client đang kết nối: " + __ClientSockets.Count.ToString();
                                lbox_Client.Items.RemoveAt(int_removeID);
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
            bt_StartSV.Content = "Ready!";
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
                Icon_Online(int_ttGK);
            }
            else if(st_case =="*re")
            {
                string _result = text.Substring(4);
                lb_status.Content = _result;
                string _pos = text.Substring(3, 1);
                string _file = _pos + ".txt";
                File.WriteAllText(_file, "");
                File.WriteAllText(_file, _result);
                update_nowResult(Convert.ToInt16(_pos));
                if (_pos == "1")
                {
                    update_result(1, st_r1);
                }
                else if(_pos =="2")
                {
                    update_result(2, st_r2);
                }
                else if (_pos == "3")
                {
                    update_result(3, st_r3);
                }
                else if (_pos == "4")
                {
                    update_result(4, st_r4);
                }
                else if (_pos == "5")
                {
                    update_result(5, st_r5);
                }
                else if (_pos == "6")
                {
                    update_result(6, st_r6);
                }
                else if (_pos == "7")
                {
                    update_result(7, st_r7);
                }
                else if (_pos == "8")
                {
                    update_result(8, st_r8);
                }

            }
        }
        void update_nowResult(int _pos)
        {
            lv_actual.Items.Clear();
            for(int i = 0; i < int_gk; i++)
            {
                if(i == (_pos-1))
                {
                    Grid _gr = new Grid();
                    _gr.Width = 50; _gr.Height = 50;
                    Ellipse _el = new Ellipse();
                    _el.Width = 49.5;
                    _el.Height = 49.5;
                    _el.Stroke = Brushes.White;
                    _el.Fill = Brushes.Green;
                    _el.StrokeThickness = 4;
                    _gr.Children.Add(_el);
                    lv_actual.Items.Add(_gr);
                }
                else 
                {
                    Grid _gr = new Grid();
                    _gr.Width = 50; _gr.Height = 50;
                    lv_actual.Items.Add(_gr);
                }
            
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
               // Icon_Online(int_ttGK);
            }));
            

        }
        void Icon_Online(int[] pos)
        {
            lv_stt.Items.Clear();
            int[] _GK_tt = new int[int_gk];
            for(int i = 0; i < int_gk; i ++)
            {
                _GK_tt[pos[i]] = pos[i];
               
            }
            for(int i = 1; i < int_gk; i ++)
            {
                if (_GK_tt[i] != 0)
                {
                    Grid _gr = new Grid();
                    _gr.Width = 50; _gr.Height = 50;
                    Ellipse _el = new Ellipse();
                    _el.Width = 49.5;
                    _el.Height = 49.5;
                    _el.Stroke = Brushes.White;
                    _el.Fill = Brushes.Green;
                    _el.StrokeThickness = 4;
                    _gr.Children.Add(_el);
                    lv_stt.Items.Add(_gr);
                }
                else
                {
                    Grid _gr = new Grid();
                    _gr.Width = 50; _gr.Height = 50;
                    Ellipse _el = new Ellipse();
                    _el.Width = 49.5;
                    _el.Height = 49.5;
                    _el.Stroke = Brushes.White;
                    _el.Fill = Brushes.Green;
                    _el.StrokeThickness = 4;
                    _el.Opacity = 0.2;
                    _gr.Children.Add(_el);
                    lv_stt.Items.Add(_gr);
                }
            }
        }

        private void bt_updateStt_Click(object sender, RoutedEventArgs e)
        {
            Icon_Online(int_ttGK);
            update_TenGK();

           // Create_GKResult();
            update_result(1, st_r1);
            update_TenChuDe();


        }
        void update_TenGK()
        {
            lv_GK.Items.Clear();
            String[] Text = File.ReadAllLines("TenGiamKhao.txt");
            for(int i = 0; i < Text.Length; i++)
            {
                Grid _gr = new Grid();
                _gr.Width = 200; _gr.Height = 50;
                Label _lb = new Label();
                _lb.Content = Text[i];
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _gr.Children.Add(_lb);
                lv_GK.Items.Add(_gr);
            }
            

        }
        void Create_GKResult()
        {
            for(int i = 1; i < int_gk; i ++)
            {
                string _file = i.ToString() + ".txt";
                if(!File.Exists(_file))
                {
                    File.WriteAllText(_file, "0-25-40-00|1-26-28-40|0-25-40-00|1-26-28-40|0-25-40-00|1-26-28-40|0-25-40-00|1-26-28-40|");
                    
                }
            }
        }

        void update_TenChuDe()
        {
            st_r0.Children.Clear();
            string _file = "tenchude.txt";
            string a = File.ReadAllText(_file);

            string[] line = a.Split('|');
            for (int i = 0; i < line.Length; i++)
            {
                Label _lb = new Label();
                _lb.Content = line[i];
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _lb.HorizontalContentAlignment = HorizontalAlignment.Center;
                _lb.Width = 180;
                st_r0.Children.Add(_lb);
            }

        }

        Ellipse Check_Done(bool _var)
        {
            Ellipse _el = new Ellipse();
            _el.Width = 30;
            _el.Height = 30;
            _el.Stroke = Brushes.White;
            
            _el.StrokeThickness = 2;
            if (_var)
            {
                _el.Fill = Brushes.Orange;
            }
            else
            {
                _el.Fill = Brushes.Blue;
            }
            return _el;
        }
        void update_result(int pos,StackPanel _gr)
        {
            _gr.Children.Clear();
            string _file = pos.ToString() + ".txt";
            string a = File.ReadAllText(_file);
            
            string[] line = a.Split('|');
            for(int i = 0; i < line.Length; i++)
            {
                string[] li = line[i].Split('-');
                for(int j = 0; j < li.Length;j++)
                {
                    string ff = li[j];
                    int_AllResult[i, j] = Convert.ToInt16(li[j]);
                    
                }
                Label _lb = new Label();
                _lb.Content = int_AllResult[i,1] + ":" + int_AllResult[i, 2] + ":" + int_AllResult[i, 3];
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _lb.HorizontalContentAlignment = HorizontalAlignment.Left;
                _lb.Width = 152;
               
                if(int_AllResult[i,0] == 0)
                {
                    _gr.Children.Add(Check_Done(false));
                }
                else
                {
                    _gr.Children.Add(Check_Done(true));
                }
                
                _gr.Children.Add(_lb);
            }
            
        }
    }
}
