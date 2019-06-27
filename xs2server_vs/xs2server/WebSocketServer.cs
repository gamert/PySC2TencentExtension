
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
 
namespace WebSocketsServer
{
    /// <summary>
    /// Socket服务端
    /// </summary>
    public class WebSocketServer : IDisposable
    {
        #region 私有变量
        /// <summary>
        /// ip
        /// </summary>
        private string _ip = string.Empty;
        /// <summary>
        /// 端口
        /// </summary>
        private int _port = 0;
        /// <summary>
        /// 服务器地址
        /// </summary>
        private string _serverLocation = string.Empty;
        /// <summary>
        /// Socket对象
        /// </summary>
        private Socket _socket = null;
        /// <summary>
        /// 监听的最大连接数
        /// </summary>
        private int maxListenConnect = 10;
        /// <summary>
        /// 是否关闭Socket对象
        /// </summary>
        private bool isDisposed = false;
 
        private Logger logger = null;
        /// <summary>
        /// buffer缓存区字节数
        /// </summary>
        private int maxBufferSize = 0;
        /// <summary>
        /// 第一个字节,以0x00开始
        /// </summary>
        private byte[] FirstByte;
        /// <summary>
        /// 最后一个字节,以0xFF结束
        /// </summary>
        private byte[] LastByte;
        #endregion
 
        #region 声明Socket处理事件
        /// <summary>
        /// Socket新连接事件
        /// </summary>
        public event NewConnection_EventHandler NewConnectionHandler;
        /// <summary>
        /// Socket接收消息事件
        /// </summary>
        public event DataReceive_EventHandler DataReceiveHandler;
        /// <summary>
        /// Socket断开连接事件
        /// </summary>
        public event Disconncetion_EventHandler DisconnectionHandler;
        #endregion
 
        /// <summary>
        /// 存放SocketConnection集合
        /// </summary>
        List<SocketConnection> SocketConnections = new List<SocketConnection>();
 
        #region 构造函数
        public WebSocketServer()
        {
            this._ip = GetLocalMachineIPAddress().ToString();
            this._port = 9000;
            this._serverLocation = string.Format("ws://{0}:{1}", this._ip, this._port);
            Initialize();
        }
        public WebSocketServer(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
            this._serverLocation = string.Format("ws://{0}:{1}", this._ip, this._port);
            Initialize();
        }
        public WebSocketServer(string ip, int port, string serverLocation)
        {
            this._ip = ip;
            this._port = port;
            this._serverLocation = serverLocation;
            Initialize();
        }
        #endregion
 
        /// <summary>
        /// 初始化私有变量
        /// </summary>
        private void Initialize()
        {
            isDisposed = false;
            logger = new Logger()
            {
                LogEvents = true
            };
            maxBufferSize = 1024 * 1024;
            maxListenConnect = 500;
            FirstByte = new byte[maxBufferSize];
            LastByte = new byte[maxBufferSize];
            FirstByte[0] = 0x00;
            LastByte[0] = 0xFF;
        }
 
        /// <summary>
        /// 开启服务
        /// </summary>
        public void StartServer()
        {
            try
            {
                //实例化套接字
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                //创建IP对象
                IPAddress address = GetLocalMachineIPAddress();
                //创建网络端点,包括ip和port
                IPEndPoint endPoint = new IPEndPoint(address, _port);
                //将socket与本地端点绑定
                _socket.Bind(endPoint);
                //设置最大监听数
                _socket.Listen(maxListenConnect);
 
                logger.Log(string.Format("聊天服务器启动。监听地址：{0}, 端口：{1}", this._ip, this._port));
                logger.Log(string.Format("WebSocket服务器地址: ws://{0}:{1}", this._ip, this._port));
 
                //开始监听客户端
                Thread thread = new Thread(ListenClientConnect);
                thread.Start();
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
            }
        }
 
        /// <summary>
        /// 监听客户端连接
        /// </summary>
        private void ListenClientConnect()
        {
            try
            {
                while (true)
                {
                    //为新建连接创建的Socket
                    Socket socket = _socket.Accept();
                    if (socket != null)
                    {
                        //线程不休眠的话,会导致回调函数的AsyncState状态出异常
                        Thread.Sleep(100);
                        SocketConnection socketConnection = new SocketConnection(this._ip, this._port, this._serverLocation)
                        {
                            ConnectionSocket = socket
                        };
                        //绑定事件
                        socketConnection.NewConnectionHandler += SocketConnection_NewConnectionHandler;
                        socketConnection.DataReceiveHandler += SocketConnection_DataReceiveHandler;
                        socketConnection.DisconnectionHandler += SocketConnection_DisconnectionHandler;
                        //从开始连接的Socket中异步接收消息
                        socketConnection.ConnectionSocket.BeginReceive(socketConnection.receivedDataBuffer,
                                        0, socketConnection.receivedDataBuffer.Length,
                                        0, new AsyncCallback(socketConnection.ManageHandshake),
                                        socketConnection.ConnectionSocket.Available);
                        //存入集合,以便在Socket发送消息时发送给所有连接的Socket套接字
                        SocketConnections.Add(socketConnection);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
 
        }
 
        /// <summary>
        /// SocketConnection监听的新连接事件
        /// </summary>
        /// <param name="loginName"></param>
        /// <param name="args"></param>
        private void SocketConnection_NewConnectionHandler(string loginName, EventArgs args)
        {
            NewConnectionHandler?.Invoke(loginName, EventArgs.Empty);
        }
        /// <summary>
        /// SocketConnection监听的消息接收事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="msgData"></param>
        /// <param name="args"></param>
        private void SocketConnection_DataReceiveHandler(object sender, string msgData, EventArgs args)
        {
            //新用户连接进来时显示欢迎信息
            //SocketConnection socketConnection = sender as SocketConnection;
            Send(msgData);
        }
        /// <summary>
        /// SocketConnection监听的断开连接事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SocketConnection_DisconnectionHandler(object sender, string message, EventArgs args)
        {
            if (sender is SocketConnection socket)
            {
                Send(message);
                socket.ConnectionSocket.Close();
                SocketConnections.Remove(socket);
            }
        }
 
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        public void Send(string message)
        {
            //给所有连接上的发送消息
            foreach (SocketConnection socket in SocketConnections)
            {
                if (!socket.ConnectionSocket.Connected)
                {
                    continue;
                }
                try
                {
                    if (socket.IsDataMasked)
                    {
                        DataFrame dataFrame = new DataFrame(message);
                        socket.ConnectionSocket.Send(dataFrame.GetBytes());
                    }
                    else
                    {
                        socket.ConnectionSocket.Send(FirstByte);
                        socket.ConnectionSocket.Send(Encoding.UTF8.GetBytes(message));
                        socket.ConnectionSocket.Send(LastByte);
                    }
                }
                catch (Exception ex)
                {
                    logger.Log(ex.Message);
                }
            }
        }
 
        /// <summary>
        /// 获取当前主机的IP地址
        /// </summary>
        /// <returns></returns>
        private IPAddress GetLocalMachineIPAddress()
        {
            //获取计算机主机名
            string hostName = Dns.GetHostName();
            //将主机名解析为IPHostEntry
            IPHostEntry hostEntry = Dns.GetHostEntry(hostName);
            foreach (IPAddress address in hostEntry.AddressList)
            {
                //IP4寻址协议
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }
            }
            return hostEntry.AddressList[0];
        }
 
        ~WebSocketServer()
        {
            Close();
        }
 
        public void Dispose()
        {
            Close();
        }
        public void Close()
        {
            if (!isDisposed)
            {
                isDisposed = true;
                if (_socket != null)
                {
                    _socket.Close();
                }
                foreach (SocketConnection socketConnection in SocketConnections)
                {
                    socketConnection.ConnectionSocket.Close();
                }
                SocketConnections.Clear();
                GC.SuppressFinalize(this);
            }
        }
    }
}
