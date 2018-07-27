using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DownLoadImage
{
    public class FtpClient
    {
        private byte[] _buffer = new byte[0x200];
        private bool _connected;
        private int _iReplyCode;
        private string _remoteHost;
        private string _remotePass;
        private string _remotePath;
        private int _remotePort = 0x15;
        private string _remoteUser;
        private Socket _socketControl;
        private string _strMsg;
        private string _strReply;
        private TransferType _trType;
        private Encoding ASCII = Encoding.Default;

        private void ChDir(string strDirName)
        {
            if (!string.IsNullOrEmpty(strDirName))
            {
                if (!this._connected)
                {
                    this.Connect();
                }
                try
                {
                    this.SendCommand("CWD " + strDirName);
                }
                catch (Exception exception)
                {
                    this.DisConnect();
                    throw exception;
                }
                if (this._iReplyCode != 250)
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                this._remotePath = strDirName;
            }
        }

        private void CloseSocketConnect()
        {
            if (this._socketControl != null)
            {
                this._socketControl.Close();
                this._socketControl = null;
            }
            this._connected = false;
        }

        private void Connect()
        {
            this._socketControl = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(this._remoteHost), this._remotePort);
            try
            {
                this._socketControl.Connect(remoteEP);
            }
            catch (Exception)
            {
                throw new IOException("连接不上FTP服务器！");
            }
            try
            {
                this.ReadReply();
                if (this._iReplyCode != 220)
                {
                    this.CloseSocketConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                this.SendCommand("USER " + this._remoteUser);
                if ((this._iReplyCode != 0x14b) && (this._iReplyCode != 230))
                {
                    this.CloseSocketConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                if (this._iReplyCode != 230)
                {
                    this.SendCommand("PASS " + this._remotePass);
                    if ((this._iReplyCode != 230) && (this._iReplyCode != 0xca))
                    {
                        this.CloseSocketConnect();
                        throw new IOException(this._strReply.Substring(4));
                    }
                }
            }
            catch
            {
                this.CloseSocketConnect();
                throw new IOException("登录用户名密码错误!");
            }
            this._connected = true;
        }

        private void Connect(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            try
            {
                string str;
                string[] strArray4;
                string[] strArray = url.Split(new char[] { '/' });
                int num = url.LastIndexOf('/');
                int index = url.Substring(6).IndexOf('/');
                this._remotePath = url.Substring(6 + index, (num - 5) - index);
                if (strArray[2].Contains("@"))
                {
                    string[] strArray2 = strArray[2].Split(new char[] { '@' });
                    string[] strArray3 = strArray2[0].Split(new char[] { ':' });
                    if (strArray3.Length > 1)
                    {
                        this._remoteUser = strArray3[0];
                        this._remotePass = strArray3[1];
                    }
                    else
                    {
                        this._remoteUser = strArray3[0];
                    }
                    if (strArray2[1].Contains(":"))
                    {
                        strArray4 = strArray2[1].Split(new char[] { ':' });
                        this._remoteHost = strArray4[0];
                        this._remotePort = int.Parse(strArray4[1]);
                    }
                    else
                    {
                        this._remoteHost = strArray2[1];
                    }
                }
                else
                {
                    this._remoteUser = "anonymous";
                    if (strArray[2].Contains(":"))
                    {
                        strArray4 = strArray[2].Split(new char[] { ':' });
                        this._remoteHost = strArray4[0];
                        this._remotePort = int.Parse(strArray4[1]);
                    }
                    else
                    {
                        this._remoteHost = strArray[2];
                    }
                }
                if ((num + 1) < url.Length)
                {
                    str = url.Substring(num + 1);
                    if (str.Contains(this._remoteHost))
                    {
                        str = "";
                    }
                }
                else
                {
                    str = "";
                }
                if (str == "")
                {
                    this.Connect();
                }
                else
                {
                    this.Connect();
                }
            }
            catch (Exception exception)
            {
                throw exception;
            }
        }

        private Socket CreateDataSocket()
        {
            this.SendCommand("PASV");
            if (this._iReplyCode != 0xe3)
            {
                this.DisConnect();
                throw new IOException(this._strReply.Substring(4));
            }
            int index = this._strReply.IndexOf('(');
            int num2 = this._strReply.IndexOf(')');
            string str = this._strReply.Substring(index + 1, (num2 - index) - 1);
            int[] numArray = new int[6];
            int length = str.Length;
            int num4 = 0;
            string s = "";
            for (int i = 0; (i < length) && (num4 <= 6); i++)
            {
                char c = char.Parse(str.Substring(i, 1));
                if (char.IsDigit(c))
                {
                    s = s + c;
                }
                else if (c != ',')
                {
                    this.DisConnect();
                    throw new IOException("Malformed PASV strReply: " + this._strReply);
                }
                if ((c == ',') || ((i + 1) == length))
                {
                    try
                    {
                        numArray[num4++] = int.Parse(s);
                        s = "";
                    }
                    catch (Exception)
                    {
                        this.DisConnect();
                        throw new IOException("Malformed PASV strReply: " + this._strReply);
                    }
                }
            }
            string ipString = string.Concat(new object[] { numArray[0], ".", numArray[1], ".", numArray[2], ".", numArray[3] });
            int port = (numArray[4] << 8) + numArray[5];
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint remoteEP = new IPEndPoint(IPAddress.Parse(ipString), port);
            try
            {
                socket.Connect(remoteEP);
            }
            catch (Exception)
            {
                this.DisConnect();
                throw new IOException("Can't connect to remote server");
            }
            return socket;
        }

        private string[] Dir(string strMask)
        {
            return this.Dir(strMask, false);
        }

        private string[] Dir(string strMask, bool isDetails)
        {
            string[] strArray;
            Exception exception;
            if (!this._connected)
            {
                this.Connect();
            }
            Socket socket = this.CreateDataSocket();
            try
            {
                int num;
                bool flag;
                if (isDetails)
                {
                    this.SendCommand("LIST " + strMask);
                }
                else
                {
                    this.SendCommand("NLST " + strMask);
                }
                if (((this._iReplyCode != 150) && (this._iReplyCode != 0x7d)) && (this._iReplyCode != 0xe2))
                {
                    return null;
                }
                this._strMsg = "";
                goto Label_00E5;
                Label_0092:
                num = socket.Receive(this._buffer, this._buffer.Length, SocketFlags.None);
                this._strMsg = this._strMsg + this.ASCII.GetString(this._buffer, 0, num);
                if (num < this._buffer.Length)
                {
                    goto Label_00EA;
                }
                Label_00E5:
                flag = true;
                goto Label_0092;
                Label_00EA:;
                char[] separator = new char[] { '\n' };
                strArray = this._strMsg.Split(separator);
            }
            catch (Exception exception1)
            {
                exception = exception1;
                this.DisConnect();
                throw exception;
            }
            finally
            {
                if (socket != null)
                {
                    socket.Close();
                    socket = null;
                }
            }
            if (this._iReplyCode != 0xe2)
            {
                try
                {
                    this.ReadReply();
                }
                catch (Exception exception2)
                {
                    exception = exception2;
                    this.DisConnect();
                    throw exception;
                }
                if (this._iReplyCode != 0xe2)
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
            }
            return strArray;
        }

        private void DisConnect()
        {
            if (this._socketControl != null)
            {
                try
                {
                    this.SendCommand("QUIT");
                }
                catch (Exception exception)
                {
                    this.CloseSocketConnect();
                    throw exception;
                }
            }
            this.CloseSocketConnect();
        }

        public void Get(string strRemoteFileName, string strFolder, string strLocalFileName)
        {
            Exception exception;
            if (!this._connected)
            {
                this.Connect(strRemoteFileName);
            }
            string fileName = Path.GetFileName(strRemoteFileName);
            this.SetTransferType(TransferType.Binary);
            if (string.IsNullOrEmpty(strLocalFileName))
            {
                strLocalFileName = fileName;
            }
            using (FileStream stream = new FileStream(strFolder + @"\" + strLocalFileName, FileMode.Create))
            {
                using (Socket socket = this.CreateDataSocket())
                {
                    try
                    {
                        this.SendCommand("RETR " + Path.Combine(this._remotePath, fileName));
                    }
                    catch (Exception exception1)
                    {
                        exception = exception1;
                        this.DisConnect();
                        throw exception;
                    }
                    if ((((this._iReplyCode != 150) && (this._iReplyCode != 0x7d)) && (this._iReplyCode != 0xe2)) && (this._iReplyCode != 250))
                    {
                        this.DisConnect();
                        throw new IOException(this._strReply.Substring(4));
                    }
                    try
                    {
                        int num;
                        bool flag;
                        goto Label_0111;
                        Label_00DA:
                        num = socket.Receive(this._buffer, this._buffer.Length, SocketFlags.None);
                        stream.Write(this._buffer, 0, num);
                        if (num <= 0)
                        {
                            goto Label_0150;
                        }
                        Label_0111:
                        flag = true;
                        goto Label_00DA;
                    }
                    catch (Exception exception2)
                    {
                        exception = exception2;
                        this.DisConnect();
                        throw exception;
                    }
                }
            }
            Label_0150:
            if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
            {
                try
                {
                    this.ReadReply();
                }
                catch (Exception exception3)
                {
                    exception = exception3;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
            }
            this.DisConnect();
        }

        public Stream GetStream(string strRemoteFileName)
        {
            Exception exception;
            Stream stream = new MemoryStream();
            if (!this._connected)
            {
                this.Connect(strRemoteFileName);
            }
            string fileName = Path.GetFileName(strRemoteFileName);
            this.SetTransferType(TransferType.Binary);
            using (Socket socket = this.CreateDataSocket())
            {
                try
                {
                    this.SendCommand("RETR " + Path.Combine(this._remotePath, fileName));
                }
                catch (Exception exception1)
                {
                    exception = exception1;
                    this.DisConnect();
                    throw exception;
                }
                if ((((this._iReplyCode != 150) && (this._iReplyCode != 0x7d)) && (this._iReplyCode != 0xe2)) && (this._iReplyCode != 250))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                try
                {
                    int num;
                    bool flag;
                    goto Label_00EF;
                    Label_00B8:
                    num = socket.Receive(this._buffer, this._buffer.Length, SocketFlags.None);
                    stream.Write(this._buffer, 0, num);
                    if (num <= 0)
                    {
                        goto Label_0118;
                    }
                    Label_00EF:
                    flag = true;
                    goto Label_00B8;
                }
                catch (Exception exception2)
                {
                    exception = exception2;
                    this.DisConnect();
                    throw exception;
                }
            }
            Label_0118:
            if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
            {
                try
                {
                    this.ReadReply();
                }
                catch (Exception exception3)
                {
                    exception = exception3;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
            }
            this.DisConnect();
            return stream;
        }

        public TransferType GetTransferType()
        {
            return this._trType;
        }

        private bool IsDir(string name)
        {
            string[] strArray;
            if (string.IsNullOrEmpty(name))
            {
                return true;
            }
            if (!this._connected)
            {
                this.Connect();
            }
            try
            {
                strArray = this.Dir("", true);
            }
            catch (Exception exception)
            {
                throw exception;
            }
            if ((strArray != null) && (strArray.Length > 0))
            {
                foreach (string str in strArray)
                {
                    if (str.Contains(name + "\r"))
                    {
                        return str.Contains("<DIR>");
                    }
                }
                this.DisConnect();
                throw new Exception("未找到指定目录或文件！");
            }
            this.DisConnect();
            throw new Exception("未找到指定目录或文件！");
        }

        public void Put(string strFileName, string remotePath)
        {
            Exception exception;
            if (!this._connected)
            {
                this.Connect(remotePath);
            }
            this.ChDir(this._remotePath);
            using (Socket socket = this.CreateDataSocket())
            {
                try
                {
                    this.SendCommand("STOR " + Path.GetFileName(strFileName));
                }
                catch (Exception exception1)
                {
                    exception = exception1;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0x7d) && (this._iReplyCode != 150))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                using (FileStream stream = new FileStream(strFileName, FileMode.Open))
                {
                    int size = 0;
                    try
                    {
                        while ((size = stream.Read(this._buffer, 0, this._buffer.Length)) > 0)
                        {
                            socket.Send(this._buffer, size, SocketFlags.None);
                        }
                    }
                    catch (Exception exception2)
                    {
                        exception = exception2;
                        this.DisConnect();
                        throw exception;
                    }
                }
            }
            if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
            {
                try
                {
                    this.ReadReply();
                }
                catch (Exception exception3)
                {
                    exception = exception3;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
            }
            this.DisConnect();
        }

        public void Put(string remotePath, byte[] data)
        {
            Exception exception;
            if (!this._connected)
            {
                this.Connect(remotePath);
            }
            this.ChDir(this._remotePath);
            string fileName = Path.GetFileName(remotePath);
            using (Socket socket = this.CreateDataSocket())
            {
                try
                {
                    this.SendCommand("STOR " + fileName);
                }
                catch (Exception exception1)
                {
                    exception = exception1;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0x7d) && (this._iReplyCode != 150))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
                try
                {
                    for (int i = 0; i < data.Length; i += 0x400)
                    {
                        byte[] buffer;
                        int num2 = i + 0x400;
                        if (num2 >= data.Length)
                        {
                            int count = data.Length % 0x400;
                            buffer = new byte[count];
                            Buffer.BlockCopy(data, i, buffer, 0, count);
                        }
                        else
                        {
                            buffer = new byte[0x400];
                            Buffer.BlockCopy(data, i, buffer, 0, 0x400);
                        }
                        socket.Send(buffer, buffer.Length, SocketFlags.None);
                    }
                }
                catch (Exception exception2)
                {
                    exception = exception2;
                    this.DisConnect();
                    throw exception;
                }
            }
            if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
            {
                try
                {
                    this.ReadReply();
                }
                catch (Exception exception3)
                {
                    exception = exception3;
                    this.DisConnect();
                    throw exception;
                }
                if ((this._iReplyCode != 0xe2) && (this._iReplyCode != 250))
                {
                    this.DisConnect();
                    throw new IOException(this._strReply.Substring(4));
                }
            }
            this.DisConnect();
        }

        private string ReadLine()
        {
            while (true)
            {
                int count = 0;
                try
                {
                    count = this._socketControl.Receive(this._buffer, this._buffer.Length, SocketFlags.None);
                }
                catch
                {
                    throw new IOException("链接失败");
                }
                this._strMsg = this._strMsg + this.ASCII.GetString(this._buffer, 0, count);
                if (count < this._buffer.Length)
                {
                    char[] separator = new char[] { '\n' };
                    string[] strArray = this._strMsg.Split(separator);
                    if (this._strMsg.Length > 2)
                    {
                        this._strMsg = strArray[strArray.Length - 2];
                    }
                    else
                    {
                        this._strMsg = strArray[0];
                    }
                    if (!this._strMsg.Substring(3, 1).Equals(" "))
                    {
                        return this.ReadLine();
                    }
                    return this._strMsg;
                }
            }
        }

        private void ReadReply()
        {
            this._strMsg = "";
            this._strReply = this.ReadLine();
            this._iReplyCode = int.Parse(this._strReply.Substring(0, 3));
        }

        private void SendCommand(string strCommand)
        {
            byte[] bytes = this.ASCII.GetBytes((strCommand + "\r\n").ToCharArray());
            try
            {
                this._socketControl.Send(bytes, bytes.Length, SocketFlags.None);
            }
            catch
            {
                throw new IOException("链接失败");
            }
            this.ReadReply();
        }

        public void SetTransferType(TransferType ttType)
        {
            try
            {
                if (ttType == TransferType.Binary)
                {
                    this.SendCommand("TYPE I");
                }
                else
                {
                    this.SendCommand("TYPE A");
                }
            }
            catch (Exception exception)
            {
                this.DisConnect();
                throw exception;
            }
            if (this._iReplyCode != 200)
            {
                this.DisConnect();
                throw new IOException(this._strReply.Substring(4));
            }
            this._trType = ttType;
        }

        public bool Connected
        {
            get
            {
                return this._connected;
            }
        }

        public string RemoteHost
        {
            get
            {
                return this._remoteHost;
            }
        }

        public string RemotePass
        {
            get
            {
                return this._remotePass;
            }
        }

        public string RemotePath
        {
            get
            {
                return this._remotePath;
            }
        }

        public int RemotePort
        {
            get
            {
                return this._remotePort;
            }
        }

        public string RemoteUser
        {
            get
            {
                return this._remoteUser;
            }
        }

        public enum TransferType
        {
            Binary,
            ASCII
        }
    }
}
