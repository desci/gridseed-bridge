using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

namespace GridseedBridge
{
    public partial class Form1 : Form
    {
        private static BackgroundWorker BackgroundSocket = new BackgroundWorker();

        public static bool Processing = false;
        public static int SumTimer = 160;
        public static int DevTimer = 150;
        public static int PoolTimer = 450;
        public static bool ShutDown = false;

        public static int BytesSent = 0;
        public static int BytesReceived = 0;
        public static int BytesSent_Cache = 0;
        public static int BytesReceived_Cache = 0;

        public static int WebBytesSent = 0;
        public static int WebBytesSent_Cache = 0;
        public static int WebBytesReceived = 0;
        public static int WebBytesReceived_Cache = 0;

        //timers
        private static System.Windows.Forms.Timer Timer1 = new System.Windows.Forms.Timer();
        private static System.Windows.Forms.Timer Timer2 = new System.Windows.Forms.Timer();

        //Edit from the config file
        public static bool logging = true;
        public static int API_Port = 4001;
        public static IPAddress API_IP = IPAddress.Parse("192.168.0.110");
        public static string Website_URI = "none";
        //endconfig

        private NotifyIcon trayIcon;
        private ContextMenu trayMenu;

        public Form1()
        {
            InitializeComponent();

            BackgroundSocket.DoWork += new DoWorkEventHandler(BackgroundSocket_DoWork);
            BackgroundSocket.RunWorkerCompleted += new RunWorkerCompletedEventHandler(BackgroundSocket_DoWorkCompleted);
            SysTray();
            this.Icon = new Icon(GridseedBridge.Properties.Resources.snake, 48, 48);

            Timer1.Enabled = false;
            Timer1.Interval = 1;
            Timer1.Tick += new EventHandler(timer1_Tick);

            Timer2.Enabled = false;
            Timer2.Interval = 100;
            Timer2.Tick += new EventHandler(timer2_Tick);

            if (!File.Exists("config.cfg"))
            {
                /*using (StreamWriter sw = File.AppendText("config.cfg"))
                {
                    sw.WriteLine("#Address of the pi on the LAN");
                    sw.WriteLine("pi=0.0.0.0");
                    sw.WriteLine("");
                    sw.WriteLine("#API port on the PI");
                    sw.WriteLine("port=4001");
                    sw.WriteLine("");
                    sw.WriteLine("#Logs restart attempts, error codes, etc to logs.txt");
                    sw.WriteLine("logging=true");
                    sw.WriteLine("");
                    sw.WriteLine("#script web address (example: http://www.mywebsite.com/incoming.php");
                    sw.WriteLine("address=none");

                    sw.Close();
                }*/
                Form form2 = new Form2();

                form2.Show();
            }
            else
            {
                string[] lines = File.ReadAllLines("config.cfg");

                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line) | !line.Contains("#"))
                    {
                        var tmp = line.Split('=');

                        if (line.Contains("pi="))
                        {
                            API_IP = IPAddress.Parse(tmp[1]);
                        }
                        else if (line.Contains("port="))
                        {
                            API_Port = Convert.ToInt32(tmp[1]);
                        }
                        else if (line.Contains("logging="))
                        {
                            logging = Convert.ToBoolean(tmp[1]);
                        }
                        else if (line.Contains("address="))
                        {
                            if (string.IsNullOrEmpty(tmp[1]) | tmp[1] == "none")
                            {
                                timer1.Enabled = false;
                                timer2.Enabled = false;

                                Form form2 = new Form2();

                                form2.Show();
                            }
                            else
                                Website_URI = tmp[1];
                        }
                    }
                }

                Timer1.Enabled = true;
                Timer2.Enabled = true;
            }
        }

        public static void FinishedConfig()
        {
            Timer1.Enabled = true;
            Timer2.Enabled = true;
        }

        private void SysTray()
        {
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Show", Tray_Show);
            trayMenu.MenuItems.Add("Exit", Tray_Close);

            trayIcon = new NotifyIcon();
            trayIcon.Text = "GridBridge";
            trayIcon.Icon = new Icon(GridseedBridge.Properties.Resources.snake, 48, 48);

            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;
        }

        private void Tray_Close(object sender, EventArgs e)
        {
            ShutDown = true;
            this.Close();
        }

        private void Tray_Show(object sender, EventArgs e)
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
        }

        private void Error_Handling(int code, string msg=null)
        {
            if (code == 10)
            {
                RequestInfo("quit");
                msg = "No devices found. Attempting restart.";
            }
            else if (code == 11)
            {
                RequestInfo("quit");
                msg = "One (1) or more devices offline. Attempting restart.";
            }
            else
            {
                if (string.IsNullOrEmpty(msg))
                    msg = "Empty error";
            }

            if (!logging)
                return;

            using (StreamWriter sw = File.AppendText(@"log.txt"))
            {
                sw.WriteLine(DateTime.Now + " :: <Code " + code + "> {");
                sw.WriteLine(msg);
                sw.WriteLine("}");
                sw.WriteLine("");

                sw.Close();
            }

        }

        private void ProcessHRData()
        {
            textBox2.Text = "";
            string UploadString = "";

            //textBox2.Text = ClientInformation.Data;

            //return;
            #region ParseTags
            string[] SummaryParse = { "MHS av", "Accepted=", "Rejected=", "Hardware Errors", "Work Utility",
                                      "Total MH", "Found Blocks", "Elapsed=" };
            string[] DevsParse = { "ID", "Enabled=", "MHS av", "Accepted=", "Rejected=", "Hardware Errors", "Utility" };
            string[] PoolsParse = { "URL=", "User=" };
            #endregion

            int DevIndex = 0;

            string[] lines = ClientInformation.Data.Split(',');

            if (ClientInformation.Query == "summary")
            {
                #region SummaryQuery
                foreach (string line in lines)
                {
                    foreach (string tmp in SummaryParse)
                    {
                        if (line.Contains(tmp))
                        {
                            if (tmp == "Accepted=" & line.Contains("Difficulty")) { continue; }
                            if (tmp == "Rejected=" & line.Contains("Difficulty")) { continue; }
                            //textBox2.Text += line + Environment.NewLine;
                            //SummaryDataParsed.Add(line.Split('=')[1]);
                            UploadString += line.Split('=')[1] + ",";
                        }
                    }
                }
                #endregion
            }
            else if (ClientInformation.Query == "pools")
            {
                #region PoolQuery
                foreach (string line in lines)
                {
                    foreach (string tmp in PoolsParse)
                    {
                        if (line.Contains(tmp))
                        {
                            if (tmp == "URL=")
                            {
                                if (string.IsNullOrEmpty(line.Split('=')[1]))
                                {
                                    UploadString += "0,";
                                    continue;
                                }
                            }

                            if (tmp == "URL=" & line.Contains("http://"))
                            {
                                var tttmp = line.Split('=')[1];
                                UploadString += tttmp.Split('/')[2] + ",";
                                continue;
                            }

                            UploadString += line.Split('=')[1] + ",";
                        }
                    }
                }
                #endregion
            }
            else if (ClientInformation.Query == "devs")
            {
                #region DevsQuery
                if (ClientInformation.Data.Contains("Code=10")) { Error_Handling(10); return; }

                foreach (string tmp in DevsParse)
                {
                    foreach (string line in lines)
                    {
                        if (line.Contains(tmp))
                        {
                            if (tmp == "Accepted=" & line.Contains("Difficulty"))
                                continue;

                            if (tmp == "Rejected=" & line.Contains("Difficulty"))
                                continue;

                            if (tmp == "Enabled=" & line.Contains("Enabled=N"))
                            {
                                Error_Handling(11);
                                return;
                            }

                            if (tmp == "ID")
                                DevIndex++;
                            else
                                UploadString += line.Split('=')[1] + "|";
                        }
                    }

                    if (tmp == "ID")
                        UploadString += DevIndex.ToString();

                    UploadString += ",";
                }
                #endregion
            }

            textBox2.Text = UploadString;

            //Processing = false;
            //return;

            if (!string.IsNullOrEmpty(UploadString))
            {
                #region UploadStats
                WebBytesSent_Cache = WebBytesSent; WebBytesSent += Encoding.Default.GetBytes(UploadString).Length;
                
                WebClient wc = new WebClient();
                string resp = "";

                try
                {
                    if (ClientInformation.Query == "summary")
                        resp = wc.DownloadString(Website_URI + "?Action=True&Args=" + UploadString + "&INPUT=sum");
                    else if (ClientInformation.Query == "devs")
                        resp = wc.DownloadString(Website_URI + "?Action=True&Args=" + UploadString + "&INPUT=ind");
                    else if (ClientInformation.Query == "pools")
                        resp = wc.DownloadString(Website_URI + "?Action=True&Args=" + UploadString + "&INPUT=pool");
                } catch { }

                WebBytesReceived_Cache = WebBytesReceived; WebBytesReceived += Encoding.Default.GetBytes(resp).Length;

                if (resp.Contains("500"))
                    textBox2.Text += Environment.NewLine + "Uploaded stats....";
                else
                    Error_Handling(0, "web response>" + resp.ToString());

                try { wc.Dispose(); } catch { }
                #endregion
            }
            Processing = false;
        }

        private void BackgroundSocket_DoWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ClientInformation.State == 4)
            { //Success process data
                ProcessHRData();
            } else {
                Processing = false;
                if (ClientInformation.State == 2) {
                    //Failed to connect...
                    textBox2.Text = "Failed to connect...";
                }
                else if (ClientInformation.State == -1) {
                    //Socket closed unexpectedly
                    textBox2.Text = "Socket closed unexpectedly";
                }
                else if (ClientInformation.State == 5) {
                    //Received data buffer was null ?????
                    textBox2.Text = "WTF DOES THIS MEAN";
                }
                else if (ClientInformation.State == 109) {
                    //Unknown error occured see clientinfo.data for exception string
                    textBox2.Text = "Unknown Exception: " + ClientInformation.Data;
                }
            }
        }

        private void BackgroundSocket_DoWork(object sender, DoWorkEventArgs e)
        {
            #region SocketCode
            string query = (string)e.Argument;

            byte[] ReadBuffer = new byte[1024 * 100];
            int Timeout = 0;
            /*
             * States:
             * -1 = Connection Closed
             * 0 = null
             * 1 = Connection good
             * 2 = Failed to connect socket (SocketExeception)
             * 3 = Data sent 
             * 4 = Data Received
             * 5 = Received Buffer null
             * 109 = Unknown error occured throw warning.
             */
            ClientInformation.State = 0;
            ClientInformation.Data = "";
            Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            //create connection
            while (true)
            {
                try
                {
                    Client.Connect(API_IP, API_Port);
                    ClientInformation.State = 1; break;
                }
                catch (SocketException sockex)
                {
                    Timeout++;
                    if (Timeout > 4)
                    {
                        Timeout = 0;
                        ClientInformation.State = 2; break;
                    }
                    Thread.Sleep(150);
                }
            }

            if (ClientInformation.State != 1) { goto CleanUp; }

            //send data
            try
            {
                byte[] SendData = Encoding.Default.GetBytes(query); BytesSent_Cache = BytesSent; BytesSent += SendData.Length;
                Client.Send(SendData);
                ClientInformation.State = 3;
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is ObjectDisposedException)
                {
                    ClientInformation.State = -1; ClientInformation.Data = "Could not send data..";
                }
                else
                {
                    ClientInformation.State = 109;
                    ClientInformation.Data = ex.ToString();
                }
            }


            if (ClientInformation.State != 3) { goto CleanUp; }

            //Receive data
            try
            {
                int bytesReceived = Client.Receive(ReadBuffer); //BytesReceived += bytesReceived;
                ClientInformation.State = 4;

                try { ClientInformation.Data = Encoding.ASCII.GetString(ReadBuffer, 0, bytesReceived); BytesReceived_Cache = BytesReceived; BytesReceived += Encoding.Default.GetBytes(ClientInformation.Data).Length; }
                catch (Exception ex) { ClientInformation.State = 109; ClientInformation.Data = "Did not receive data"; }
            }
            catch (Exception ex)
            {
                if (ex is SocketException || ex is ObjectDisposedException)
                {
                    ClientInformation.State = -1; ClientInformation.Data = ex.ToString();
                }
                else if (ex is ArgumentNullException)
                {
                    ClientInformation.State = 5;
                }
                else
                {
                    ClientInformation.State = 109;
                    ClientInformation.Data = ex.ToString();
                }
            }

            ClientInformation.Query = query;

            //cleanup
            CleanUp:
            while (true)
            {
                if (Client.Connected)
                {
                    try { Client.Close(); } catch { }
                    Thread.Sleep(100);
                }
                else
                {
                    break;
                }
            }

            try { Client.Dispose(); } catch { }
            try { ReadBuffer = null; } catch { }
            #endregion
        }

        private static void RequestInfo(string query)
        {
            if (!BackgroundSocket.IsBusy)
            {
                BackgroundSocket.RunWorkerAsync(query);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!BackgroundSocket.IsBusy)
            {
                Processing = true;
                RequestInfo(textBox1.Text);
            }
        }

        private static class ClientInformation
        {
            public static int State { get; set; }
            public static string Data { get; set; }
            public static string Query { get; set; }
        }

        public string Size(int bytes)
        {
            string v;

            if (bytes > 1073741824)
                v = (bytes / 1073741824).ToString() + " GB";
            else if (bytes > 1048576 & bytes < 1073741824)
                v = (bytes / 1048576).ToString() + " MB";
            else if (bytes > 1024 & bytes < 1048576)
                v = (bytes / 1024).ToString() + " KB";
            else
                v = bytes.ToString() + " Bytes";

            return v;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (BytesSent_Cache != BytesSent)
            {
                BytesSent_Cache = BytesSent;

                label1.Text = "Lan out: " + Size(BytesSent);
            }

            if (BytesReceived_Cache != BytesReceived)
            {
                BytesReceived_Cache = BytesReceived;

                label2.Text = "Lan in: " + Size(BytesReceived);
            }

            if (WebBytesSent_Cache != WebBytesSent)
            {
                WebBytesSent_Cache = WebBytesSent;

                label3.Text = Size(WebBytesSent) + " :Web Out";
            }

            if (WebBytesReceived_Cache != WebBytesReceived)
            {
                WebBytesReceived_Cache = WebBytesReceived;

                label4.Text = Size(WebBytesReceived) + " :Web In";
            }
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            //return;
            SumTimer++;
            DevTimer++;
            PoolTimer++;

            if (SumTimer >= 160)
            {
                SumTimer = 0;
                if (!Processing)
                {
                    Processing = true;
                    if (!BackgroundSocket.IsBusy)
                        RequestInfo("summary");
                }
                else
                {
                    SumTimer = 140;
                }
            }

            if (DevTimer >= 150)
            {
                DevTimer = 0;
                if (!Processing)
                {
                    Processing = true;
                    if (!BackgroundSocket.IsBusy)
                        RequestInfo("devs");
                }
                else
                {
                    DevTimer = 130;
                }
            }

            if (PoolTimer >= 450)
            {
                PoolTimer = 0;
                if (!Processing)
                {
                    Processing = true;
                    if (!BackgroundSocket.IsBusy)
                        RequestInfo("pools");
                }
                else
                {
                    PoolTimer = 420;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing) { trayIcon.Dispose(); return; }
            if (ShutDown) { trayIcon.Dispose(); return; }

            this.Visible = false;
            this.ShowInTaskbar = false;

            e.Cancel = true;
        }
    }
}
