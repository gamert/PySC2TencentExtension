using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
 
namespace WebSocketsServer
{
    /// <summary>
    /// Socket�ɹ�����������
    /// </summary>
    public class SocketConnection
    {
        /// <summary>
        /// �µ�Socket����
        /// </summary>
        public Socket ConnectionSocket = null;
 
        #region Socket�����¼�
        /// <summary>
        /// �������¼�
        /// </summary>
        public event NewConnection_EventHandler NewConnectionHandler;
        /// <summary>
        /// ���ݽ����¼�
        /// </summary>
        public event DataReceive_EventHandler DataReceiveHandler;
        /// <summary>
        /// �Ͽ������¼�
        /// </summary>
        public event Disconncetion_EventHandler DisconnectionHandler;
        #endregion
 
        #region ˽�б���
        private string _ip = string.Empty;
        private int _port = 0;
        private string _serverLocation = string.Empty;
 
        private Logger logger;
 
        private string loginId;
        public string LoginId
        {
            get => loginId; set => loginId = value;
        }
        private bool isDataMasked;
        public bool IsDataMasked { get => isDataMasked; set => isDataMasked = value; }
        /// <summary>
        /// ��󻺴����ֽ���
        /// </summary>
        private int maxBufferSize = 0;
        /// <summary>
        /// ����Э����Ϣ
        /// </summary>
        private string handshake = string.Empty;
        /// <summary>
        /// ����Э����Ϣ(new)
        /// </summary>
        private string newHandshake = string.Empty;
        /// <summary>
        /// ������Ϣ�����ݻ�����
        /// </summary>
        public byte[] receivedDataBuffer;
        private byte[] firstByte;
        private byte[] lastByte;
        private byte[] serverKey1;
        private byte[] serverKey2;
        #endregion
 
        #region ���캯��
        public SocketConnection()
        {
            Initialize();
        }
 
        public SocketConnection(string ip, int port, string serverLocation)
        {
            this._ip = ip;
            this._port = port;
            this._serverLocation = serverLocation;
            Initialize();
        }
        #endregion
 
        /// <summary>
        /// ��ʼ������
        /// </summary>
        private void Initialize()
        {
            logger = new Logger();
            maxBufferSize = 1024 * 1024;
            receivedDataBuffer = new byte[maxBufferSize];
            firstByte = new byte[maxBufferSize];
            lastByte = new byte[maxBufferSize];
            firstByte[0] = 0x00;
            lastByte[0] = 0xFF;
 
            //webSocketЯ��ͷ��Ϣ
            handshake = "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine;
            handshake += "Upgrade: WebSocket" + Environment.NewLine;
            handshake += "Connection: Upgrade" + Environment.NewLine;
            handshake += "Sec-WebSocket-Origin: " + "{0}" + Environment.NewLine;
            handshake += string.Format("Sec-WebSocket-Location: " + "ws://{0}:{1}" + Environment.NewLine, this._ip, this._port);
            handshake += Environment.NewLine;
 
            newHandshake = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine;
            newHandshake += "Upgrade: WebSocket" + Environment.NewLine;
            newHandshake += "Connection: Upgrade" + Environment.NewLine;
            newHandshake += "Sec-WebSocket-Accept: {0}" + Environment.NewLine;
            newHandshake += Environment.NewLine;
        }
 
        /// <summary>
        /// �����첽������Ϣ�ص�����
        /// </summary>
        /// <param name="asyncResult"></param>
        public void ManageHandshake(IAsyncResult asyncResult)
        {
            try
            {
                string header = "Sec-WebSocket-Version:";
                int HandshakeLength = (int)asyncResult.AsyncState;
                byte[] last8Bytes = new byte[8];
 
                UTF8Encoding encoding = new UTF8Encoding();
                String rawClientHandshake = encoding.GetString(receivedDataBuffer, 0, HandshakeLength);
 
                Array.Copy(receivedDataBuffer, HandshakeLength - 8, last8Bytes, 0, 8);
                //����ʹ�õ��ǱȽ��µ�WebSocketЭ��
                if (rawClientHandshake.IndexOf(header) != -1)
                {
                    this.isDataMasked = true;
                    string[] rawClientHandshakeLines = rawClientHandshake.Split(new string[] { Environment.NewLine }, System.StringSplitOptions.RemoveEmptyEntries);
 
                    string acceptKey = "";
                    foreach (string line in rawClientHandshakeLines)
                    {
                        if (line.Contains("Sec-WebSocket-Key:"))
                        {
                            acceptKey = ComputeWebSocketHandshakeSecurityHash09(line.Substring(line.IndexOf(":") + 2));
                        }
                    }
                    newHandshake = string.Format(newHandshake, acceptKey);
                    byte[] newHandshakeText = Encoding.UTF8.GetBytes(newHandshake);
                    //�������첽���͵����ӵ�socket��
                    ConnectionSocket.BeginSend(newHandshakeText, 0, newHandshakeText.Length, SocketFlags.None, HandshakeFinished, null);
                    return;
                }
 
                string clientHandshake = encoding.GetString(receivedDataBuffer, 0, receivedDataBuffer.Length - 8);
                string[] clientHandshakeLines = clientHandshake.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
 
                logger.Log("�µ�������������:" + ConnectionSocket.LocalEndPoint + ".��׼����������...");
 
                // Welcome the new client
                foreach (string Line in clientHandshakeLines)
                {
                    logger.Log(Line);
                    if (Line.Contains("Sec-WebSocket-Key1:"))
                        BuildServerPartialKey(1, Line.Substring(Line.IndexOf(":") + 2));
                    if (Line.Contains("Sec-WebSocket-Key2:"))
                        BuildServerPartialKey(2, Line.Substring(Line.IndexOf(":") + 2));
                    if (Line.Contains("Origin:"))
                        try
                        {
                            handshake = string.Format(handshake, Line.Substring(Line.IndexOf(":") + 2));
                        }
                        catch
                        {
                            handshake = string.Format(handshake, "null");
                        }
                }
                //Ϊ�ͻ��˽�����Ӧ
                byte[] handshakeText = Encoding.UTF8.GetBytes(handshake);
                byte[] serverHandshakeResponse = new byte[handshakeText.Length + 16];
                byte[] serverKey = BuildServerFullKey(last8Bytes);
                Array.Copy(handshakeText, serverHandshakeResponse, handshakeText.Length);
                Array.Copy(serverKey, 0, serverHandshakeResponse, handshakeText.Length, 16);
 
                logger.Log("����������Ϣ ...");
                ConnectionSocket.BeginSend(serverHandshakeResponse, 0, handshakeText.Length + 16, 0, HandshakeFinished, null);
                logger.Log(handshake);
 
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
 
        /// <summary>
        /// �ɷ������ͻ��˷�����Ϣ��ɻص�
        /// </summary>
        /// <param name="asyncResult"></param>
        private void HandshakeFinished(IAsyncResult asyncResult)
        {
            //����������첽����
            ConnectionSocket.EndSend(asyncResult);
            ConnectionSocket.BeginReceive(receivedDataBuffer, 0, receivedDataBuffer.Length,
                0, new AsyncCallback(Read), null);
            NewConnectionHandler?.Invoke("", EventArgs.Empty);
        }
 
        private void Read(IAsyncResult asyncResult)
        {
            if (!ConnectionSocket.Connected)
            {
                return;
            }
            string message = string.Empty;
            DataFrame dataFrame = new DataFrame(receivedDataBuffer);
            try
            {
                if (!this.isDataMasked)
                {
                    //WebSocketЭ��:��Ϣ��0x00��0xFF��Ϊ����ֽڷ���
                    UTF8Encoding encoding = new UTF8Encoding();
                    int startIndex = 0;
                    int endIndex = 0;
 
                    // Search for the start byte
                    while (receivedDataBuffer[startIndex] == firstByte[0])
                    {
                        startIndex++;
                    }
                    // Search for the end byte
                    endIndex = startIndex + 1;
                    while (receivedDataBuffer[endIndex] != lastByte[0] && endIndex != maxBufferSize - 1)
                    {
                        endIndex++;
                    }
                    if (endIndex == maxBufferSize - 1)
                    {
                        endIndex = maxBufferSize;
                    }
                    // Get the message
                    message = encoding.GetString(receivedDataBuffer, startIndex, endIndex - startIndex);
                }//if
                else
                {
                    message = dataFrame.Text;
                }
 
                if ((message.Length == maxBufferSize && message[0] == Convert.ToChar(65533)) ||
                      message.Length == 0)
                {
                    //�Ͽ�����
                    logger.Log("message");
                    if (string.IsNullOrEmpty(message))
                    {
                        MessageInfo messageInfo = new MessageInfo()
                        {
                            MsgType = MessageType.None,
                            Message = ""
                        };
                        message = JsonConvert.SerializeObject(messageInfo);
                    }
                    DisconnectionHandler?.Invoke(this, message, EventArgs.Empty);
                }
                else
                {
                    if (DataReceiveHandler != null)
                    {
                        logger.Log("���ܵ�����Ϣ [\"" + message + "\"]");
                        //��Ϣ����
                        DataReceiveHandler(this, message, EventArgs.Empty);
                    }
                    Array.Clear(receivedDataBuffer, 0, receivedDataBuffer.Length);
                    ConnectionSocket.BeginReceive(receivedDataBuffer, 0, receivedDataBuffer.Length, 0, Read, null);
                }
            }
            catch (Exception ex)
            {
                logger.Log(ex.Message);
                logger.Log("Socket���ӽ��ᱻ��ֹ.");
                MessageInfo messageInfo = new MessageInfo()
                {
                    MsgType = MessageType.Error,
                    Message = ex.Message + Environment.NewLine + "Socket���ӽ��ᱻ��ֹ"
                };
                DisconnectionHandler?.Invoke(this, JsonConvert.SerializeObject(messageInfo), EventArgs.Empty);
            }
        }
 
        private byte[] BuildServerFullKey(byte[] last8Bytes)
        {
            byte[] concatenatedKeys = new byte[16];
            Array.Copy(serverKey1, 0, concatenatedKeys, 0, 4);
            Array.Copy(serverKey2, 0, concatenatedKeys, 4, 4);
            Array.Copy(last8Bytes, 0, concatenatedKeys, 8, 8);
 
            // MD5 Hash
            MD5 MD5Service = MD5.Create();
            return MD5Service.ComputeHash(concatenatedKeys);
        }
 
        private void BuildServerPartialKey(int keyNum, string clientKey)
        {
            string partialServerKey = "";
            byte[] currentKey;
            int spacesNum = 0;
            char[] keyChars = clientKey.ToCharArray();
            foreach (char currentChar in keyChars)
            {
                if (char.IsDigit(currentChar)) partialServerKey += currentChar;
                if (char.IsWhiteSpace(currentChar)) spacesNum++;
            }
            try
            {
                currentKey = BitConverter.GetBytes((int)(Int64.Parse(partialServerKey) / spacesNum));
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(currentKey);
                }
 
                if (keyNum == 1)
                {
                    serverKey1 = currentKey;
                }
                else
                {
                    serverKey2 = currentKey;
                }
            }
            catch
            {
                if (serverKey1 != null)
                {
                    Array.Clear(serverKey1, 0, serverKey1.Length);
                }
                if (serverKey2 != null)
                {
                    Array.Clear(serverKey2, 0, serverKey2.Length);
                }
            }
        }
 
        private string ComputeWebSocketHandshakeSecurityHash09(string secWebSocketKey)
        {
            const String MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            String secWebSocketAccept = String.Empty;
            // 1. Combine the request Sec-WebSocket-Key with magic key.
            String ret = secWebSocketKey + MagicKEY;
            // 2. Compute the SHA1 hash
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            // 3. Base64 encode the hash
            secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }
    }
}
