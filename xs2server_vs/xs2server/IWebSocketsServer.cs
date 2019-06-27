
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace WebSocketsServer
{
    /// <summary>
    /// 声明新连接处理事件
    /// </summary>
    /// <param name="loginName"></param>
    /// <param name="e"></param>
    public delegate void NewConnection_EventHandler(string loginName, EventArgs args);
 
    /// <summary>
    /// 声明接收数据处理事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public delegate void DataReceive_EventHandler(object sender, string message, EventArgs args);
 
    /// <summary>
    /// 声明断开连接处理事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void Disconncetion_EventHandler(object sender, string message, EventArgs args);
}
