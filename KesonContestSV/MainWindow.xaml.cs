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
        int[,] int_remove = new int[11, 9];
        bool[] bo_selectedGK = new bool[12];
        string[] st_Result = new string[20];
        String st_AllSetupData;
        int int_removeID;
        int[] int_ClientSend = new int[11];
        int[] int_ClientListSend = new int[11];
        int int_gk = 0;
        int[,] int_KetQua = new int[10,2];
        int[] int_ttGK = new int[20];
        private const int PORT = 197;
        IPAddress address = IPAddress.Parse("10.12.20.25");
        int[,,] int_AllResult = new int[12,8, 4]; //GK,Theme,Value

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
            ReadSetupData();
            Startup();
        }

        #region SetupServer

        //Read Address and open Socket Server
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

        //Accept Client
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

        //Remove Client
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

                    if (text.Substring(0, 3) == "*da")
                    {
                        Sendata(socket, st_AllSetupData);
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
                                int_curGk--;
                                lbox_Client.Items.RemoveAt(int_removeID);
                                Icon_Online(int_ttGK);
                            }));
                            update_GK(i);
                            __ClientSockets.RemoveAt(i);
                        }
                    }
                }

                
            }
            socket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
        }

        //Send data to directly client
        void Sendata(Socket socket, string noidung)
        {
            byte[] data = Encoding.ASCII.GetBytes(noidung);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, new AsyncCallback(SendCallback), socket);
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), null);
        }

        //Void support for Send data to client
        private void SendCallback(IAsyncResult AR)
        {
            Socket socket = (Socket)AR.AsyncState;
            socket.EndSend(AR);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SetupServer();
            
            bt_StartSV.Content = "Ready!";
            gr_ServerOnline.Opacity = 1;
            bt_StartSV.Visibility = Visibility.Hidden;
            bt_SendData.Visibility = Visibility.Visible;
        }
        void ReadSetupData()
        {
            st_AllSetupData = File.ReadAllText("data.txt");

        }
        void update_remove()
        {

            string[] line = File.ReadAllLines("remove.txt");
            try
            {
                for (int i = 0; i < line.Length; i++)
                {
                    string[] _val = line[i].Split('|');
                    for (int j = 0; j < _val.Length; j++)
                    {
                        int_remove[i+1, j] = Convert.ToInt16(_val[j]);
                    }
                }
            }
            catch(Exception ee)
            {
                MessageBox.Show(ee.ToString());
            }

        }
        void Startup()
        {
            update_TenGK();
            update_remove();
            Icon_Online(int_ttGK);


            // Create_GKResult();
            update_result(1, st_r1);
            update_result(2, st_r2);
            update_result(3, st_r3);
            update_result(4, st_r4);
            update_result(5, st_r5);
            update_result(6, st_r6);
            update_result(7, st_r7);
            update_result(8, st_r8);
            update_result(9, st_r9);
            update_result(10, st_r10);

            update_TenChuDe();
        }
        #endregion

        #region Caculate

        //Update new result from client.
        void update_result(int pos, StackPanel _gr)
        {
            _gr.Children.Clear();
            string _file = pos.ToString("00") + ".txt";
            string a = File.ReadAllText(_file);
            try
            {
                string[] line = a.Split('|');
                for (int i = 0; i < 8; i++)
                {
                    string[] li = line[i].Split('-');
                    for (int j = 0; j < 4; j++)
                    {
                        string ff = li[j];
                        int aa = Convert.ToInt16(li[j]);
                        int_AllResult[pos, i, j] = Convert.ToInt16(li[j]);

                    }
                    Label _lb = new Label();
                    _lb.Content = int_AllResult[pos, i, 1] + ":" + int_AllResult[pos, i, 2] + ":" + int_AllResult[pos, i, 3];
                    _lb.Foreground = Brushes.White;
                    _lb.Background = null;
                    _lb.FontSize = 30;
                    _lb.HorizontalContentAlignment = HorizontalAlignment.Left;
                    _lb.Width = 152;

                    if (int_AllResult[pos, i, 0] == 0)
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
            catch
            {
                Console.WriteLine("Errorrrrrrrrrrrrrrrrrrrrrrrrrrrrrrr!");
            }

            sum_result();

        }

        // Sum result and caculate the average value
        void sum_result()
        {

            for (int j = 0; j < 8; j++)
            {
                int_KetQua[j, 0] = 0;
                int_KetQua[j, 1] = 0;
                for (int i = 1; i < int_gk +1; i++)
                {
                    int a = int_AllResult[i, j, 0];
                    if ((int_AllResult[i, j, 0] == 1) && (int_remove[i,j] == 1))
                    {
                        int_KetQua[j, 0] += int_AllResult[i, j, 1] + int_AllResult[i, j, 2] + int_AllResult[i, j, 3];
                        int_KetQua[j, 1]++;
                    }
                }

            }

            st_rkq.Children.Clear();
            for (int i = 0; i < 8; i++)
            {
                Label _lb = new Label();
                double aa = 0;
                if (int_KetQua[i, 1] > 0)
                {
                    aa = (double)int_KetQua[i, 0] / int_KetQua[i, 1];
                }
                _lb.Content =aa.ToString("0.00");
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _lb.HorizontalContentAlignment = HorizontalAlignment.Center;
                _lb.Width = 180;
                st_rkq.Children.Add(_lb);
            }
            // Count

            st_rcount.Children.Clear();
            for (int i = 0; i < 8; i++)
            {
                Label _lb = new Label();
                double aa = 0;
                if (int_KetQua[i, 1] > 0)
                {
                    aa = (double)int_KetQua[i, 0] / int_KetQua[i, 1];
                }
                _lb.Content = int_KetQua[i, 0] + " : " + int_KetQua[i, 1];
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _lb.HorizontalContentAlignment = HorizontalAlignment.Center;
                _lb.Width = 180;
                st_rcount.Children.Add(_lb);
            }

        }

        //If receive a text from client. We must classify the case. So on process for each case
        void XuLyText(string text)
        {
            string st_case = text.Substring(0, 3);
            if (st_case == "*cn")
            {
                //lb_status.Content = text.Substring(3, 1) + " - Connected!";
                int_ttGK[int_curGk - 1] = Convert.ToInt16(text.Substring(3, 2));
                string a = "";
                for (int i = 0; i < int_gk; i++)
                {
                    a += int_ttGK[i] + "\t";
                }
                lb_status.Content = a;
                Icon_Online(int_ttGK);
            }
            else if (st_case == "*re")
            {
                string _result = text.Substring(5);
                //lb_status.Content = _result;
                string _pos = text.Substring(3, 2);
                string _file = _pos + ".txt";
                File.WriteAllText(_file, _result);
                update_nowResult(Convert.ToInt16(_pos));
                if (_pos == "01")
                {
                    update_result(1, st_r1);
                }
                else if (_pos == "02")
                {
                    update_result(2, st_r2);
                }
                else if (_pos == "03")
                {
                    update_result(3, st_r3);
                }
                else if (_pos == "04")
                {
                    update_result(4, st_r4);
                }
                else if (_pos == "05")
                {
                    update_result(5, st_r5);
                }
                else if (_pos == "06")
                {
                    update_result(6, st_r6);
                }
                else if (_pos == "07")
                {
                    update_result(7, st_r7);
                }
                else if (_pos == "08")
                {
                    update_result(8, st_r8);
                }
                else if (_pos == "09")
                {
                    update_result(9, st_r9);
                }
                else if (_pos == "10")
                {
                    update_result(10, st_r10);
                }
            }
        }

        //Suport for XuLyText void
        void update_nowResult(int _pos)
        {
            lv_actual.Items.Clear();
            for (int i = 0; i < int_gk; i++)
            {
                if (i == (_pos - 1))
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

        #endregion

        #region GUI

        //Update icon which notify the online client
        void Icon_Online(int[] pos)
        {
            lv_stt.Items.Clear();
            string xa = "";
            int[] _GK_tt = new int[int_gk];
            for (int i = 0; i < int_gk; i++)
            {
                _GK_tt[pos[i]] = pos[i];

            }
            for (int i = 1; i < int_gk; i++)
            {
                if (_GK_tt[i] != 0)
                {
                    if (bo_selectedGK[i])
                    {
                        Grid _gr = new Grid();
                        _gr.Width = 50; _gr.Height = 50;
                        Ellipse _el = new Ellipse();
                        _el.Width = 49.5;
                        _el.Height = 49.5;
                        _el.Stroke = Brushes.White;
                        _el.Fill = Brushes.Orange;
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
                        _gr.Children.Add(_el);
                        lv_stt.Items.Add(_gr);
                    }
                }
                else
                {
                    bo_selectedGK[i] = false;
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
            for (int k = 0; k < bo_selectedGK.Length; k++)
            {
                xa += bo_selectedGK[k].ToString() + "\t";
            }
            Console.WriteLine(xa);
            Client_Send();
        }

        //Update Judges name
        void update_TenGK()
        {
            lv_GK.Items.Clear();
            String[] Text = File.ReadAllLines("TenGiamKhao.txt");
            for (int i = 0; i < Text.Length; i++)
            {
                Grid _gr = new Grid();
                _gr.Width = 150; _gr.Height = 50;
                Label _lb = new Label();
                _lb.Content = Text[i];
                _lb.Foreground = Brushes.White;
                _lb.Background = null;
                _lb.FontSize = 30;
                _gr.Children.Add(_lb);
                lv_GK.Items.Add(_gr);
            }
            int_gk = Text.Length + 1;


        }

        //Update Theme name
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

        //Update sort of Judges
        void update_GK(int vt)
        {
            string a = "";
            for (int i = (vt); i < int_gk; i++)
            {
                int_ttGK[i] = int_ttGK[i + 1];
            }
            for (int i = 0; i < int_gk; i++)
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

        // Just create a round shape
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

        #endregion

        #region interface

        //Select the Client to send data
        void Client_Send()
        {
            for (int i = 0; i < int_ClientListSend.Length; i++)
            {
                int_ClientListSend[i] = -1;
            }
            int k = 1;
            string aa = "";
            for (int i = 1; i < int_gk + 1; i++)
            {
                if (bo_selectedGK[i])
                {
                    for (int j = 0; j < int_gk; j++)
                    {
                        if (i == int_ttGK[j])
                        {
                            int_ClientListSend[k++] = j;
                        }
                    }
                }
            }
            for (int i = 1; i < int_ClientListSend.Length; i++)
            {
                aa += int_ClientListSend[i] + "\t";
            }
            Console.WriteLine(aa);

        }

        //Send data to client
        private void bt_SendData_Click(object sender, RoutedEventArgs e)
        {

            for (int i = 1; i < int_ClientListSend.Length; i++)
            {
                if (int_ClientListSend[i] != -1)
                {
                    Sendata(__ClientSockets[int_ClientListSend[i]]._Socket, tb_DataToSend.Text + "*not\r\n");
                }
            }
        }

        //Select the online client to send
        private void lv_GK_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lv_GK.SelectedIndex != -1)
            {
                int a = lv_GK.SelectedIndex;
                a++;
                if (bo_selectedGK[a])
                {
                    bo_selectedGK[a] = false;
                }
                else
                {
                    bo_selectedGK[a] = true;
                }
                Icon_Online(int_ttGK);
                lv_GK.SelectedIndex = -1;
            }

        }

        //Open Theme 2
        private void bt_Next_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i < int_ClientListSend.Length; i++)
            {
                if (int_ClientListSend[i] != -1)
                {
                    Sendata(__ClientSockets[int_ClientListSend[i]]._Socket,  "aa*nex\r\n");
                }
            }
        }
        #endregion

        private void bt_SaveResult_Click(object sender, RoutedEventArgs e)
        {
            string kq = "";
            for(int i = 0; i < 8; i ++)
            {
                kq  += ((double)int_KetQua[i, 0] / int_KetQua[i, 1]).ToString("00.00") + "\r\n";
            }
            File.WriteAllText("KetQuaThiDau.txt", kq);
        }
    }
}
