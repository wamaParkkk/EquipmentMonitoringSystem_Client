using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace EquipmentMonitoringSystem_Client
{
    public partial class ClientForm : Form
    {
        private Socket clientSocket = null;
        private Socket cbSocket;
        private byte[] recvBuffer;
        private const int MAXSIZE = 4096;
        private string HostIPAddress = "10.141.139.40";   // 오버홀룸 테스트PC
        //private string HostIPAddress = "10.141.216.71";
        //private string HostIPAddress = "10.141.217.224";    // 내 노트북
        private int Port = 8000;
        delegate void ctrl_Invoke(RichTextBox ctrl, string sMsg, string Netmessage);

        private Timer displayTimer = new Timer();

        Random randomObj = new Random();
        int iCnt = 1;

        public ClientForm()
        {
            InitializeComponent();

            // Data receive buffer
            recvBuffer = new byte[MAXSIZE];

            // Server에 연결
            this._Init_Connect();
        }

        private void ClientForm_Load(object sender, EventArgs e)
        {
            displayTimer.Interval = 500;
            displayTimer.Elapsed += new ElapsedEventHandler(_Display);            
        }

        private void _Display(object sender, ElapsedEventArgs e)
        {   /*                   
            int randomValue;

            if (iCnt <= 1)
            {
                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("B,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("C,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                iCnt++;
            }
            else if (iCnt <= 2)
            {
                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("D,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("E,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                iCnt++;
            }
            else if (iCnt <= 3)
            {
                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("F,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("G,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                iCnt++;
            }
            else if (iCnt <= 4)
            {
                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("H,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("I,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                iCnt++;
            }
            else if (iCnt <= 5)
            {
                randomValue = randomObj.Next(-45, 25);
                _Begin_Send(string.Format("J,{0},{1},{2},{3};", randomValue.ToString(), "Open", "Open", "Open"));

                iCnt = 1;
            }
            */
        }

        public void _Init_Connect()
        {
            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this._Begin_Connect();
        }
        
        public void _Begin_Connect()
        {
            DisplayText("Waiting for server connection...");

            try
            {
                // 비동기 방식
                clientSocket.BeginConnect(HostIPAddress, Port, new AsyncCallback(ConnectCallBack), clientSocket);
            }
            catch (SocketException se)
            {
                DisplayText(se.Message);
                this._Init_Connect();
            }
        }

        /*
         * Connection attempt callback function
         */
        private void ConnectCallBack(IAsyncResult iAsyncResult)
        {
            try
            {
                // 보류 중인 연결을 완료
                Socket socket = (Socket)iAsyncResult.AsyncState;
                IPEndPoint iPEndPoint = (IPEndPoint)socket.RemoteEndPoint;

                DisplayText("Server connection successful " + "<" + iPEndPoint.Address + ">");

                socket.EndConnect(iAsyncResult);
                cbSocket = socket;
                // 비동기식 Data receive
                cbSocket.BeginReceive(this.recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSocket);

                displayTimer.Start();
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.NotConnected)
                {
                    DisplayText("Server connection failure (CallBack) : " + se.Message);                    
                }
                else
                {
                    DisplayText(se.Message);                    
                }

                displayTimer.Stop();

                this._Init_Connect();
            }
        }

        public void _Begin_Send(string message)
        {
            try
            {
                // 접속 상태 확인
                if (clientSocket.Connected)
                {
                    byte[] buffer = Encoding.Unicode.GetBytes(message);
                    // 비동기 전송
                    clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallBack), message);                    
                }
            }
            catch (SocketException se)
            {
                DisplayText("Send error : " + se.Message);
            }
        }

        /*
         * Data send callback function
         */
        private void SendCallBack(IAsyncResult iAsyncResult)
        {
            string strMessage = (string)iAsyncResult.AsyncState;
            //DisplayText("Send complete (CallBack) : " + strMessage);
        }

        public void Receive()
        {
            // 비동기 Data receive
            cbSocket.BeginReceive(this.recvBuffer, 0, recvBuffer.Length, SocketFlags.None, new AsyncCallback(OnReceiveCallBack), cbSocket);
        }

        /*
         * Data receive callback function
         */
        private void OnReceiveCallBack(IAsyncResult iAsyncResult)
        {
            try
            {
                Socket socket = (Socket)iAsyncResult.AsyncState;
                int nReadSize = socket.EndReceive(iAsyncResult);
                if (nReadSize != 0)
                {
                    string sData = Encoding.Unicode.GetString(recvBuffer, 0, recvBuffer.Length);
                    DisplayText("Recv data (CallBack) : " + sData);
                    //_DATA_PARSING(sData);
                }

                this.Receive();
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode == SocketError.ConnectionReset)
                {
                    this._Begin_Connect();
                }
                else
                {
                    DisplayText(se.Message);
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            _Begin_Send(textBoxSendMsg.Text);

            textBoxSendMsg.Clear();
            textBoxSendMsg.Focus();
        }

        private void textBoxSendMsg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                _Begin_Send(textBoxSendMsg.Text);

                textBoxSendMsg.Clear();
                textBoxSendMsg.Focus();
            }
        }

        private void DisplayText(string text) 
        {
            if (richTextBoxMsg.InvokeRequired)
            {
                richTextBoxMsg.BeginInvoke(new MethodInvoker(delegate
                {
                    if (richTextBoxMsg.Lines.Length > 100)
                    {
                        richTextBoxMsg.Clear();
                    }

                    richTextBoxMsg.AppendText(Environment.NewLine + "[ " + DateTime.Now.ToString() + "] " + " >> " + text);
                    richTextBoxMsg.ScrollToCaret();
                }));
            }
            else
            {
                richTextBoxMsg.AppendText(Environment.NewLine + "[ " + DateTime.Now.ToString() + "] " + " >> " + text);
                richTextBoxMsg.ScrollToCaret();
            }
        }        
    }
}
