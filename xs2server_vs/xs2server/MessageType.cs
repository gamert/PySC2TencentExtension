using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
namespace WebSocketsServer
{
    public enum MessageType
    {
        Error = -1,
        None = 0,
        /// <summary>
        /// 登录
        /// </summary>
        Login = 1,
        /// <summary>
        /// 退出
        /// </summary>
        Logout = 2,
        /// <summary>
        /// 聊天消息
        /// </summary>
        ChatInfo = 3,
    }
}
