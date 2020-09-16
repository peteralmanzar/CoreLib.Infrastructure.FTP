using System;
using System.Net;
using System.Security;

namespace CoreLib.Infrastructure.FTP
{
    public class ConnectionInfo
    {
        #region Properties
        public ConnectionType ConnectionType { get; private set; }
        public string Host { get; private set; }
        public int? Port { get; private set; }
        public string Path { get; set; } = "";
        public string Username { get; private set; }
        public string Password 
        {
            get => new NetworkCredential("", _password).Password;
            private set => _password = new NetworkCredential("", value).SecurePassword;
        }
        public SecureString SecurePassword
        {
            get => _password;
            private set => _password = value;
        }
        #endregion

        #region Fields
        private SecureString _password;
        #endregion

        #region Constructors
        public ConnectionInfo(string host, string username, string password, ConnectionType connectionType = ConnectionType.FTP)
        {
            if (string.IsNullOrEmpty(host)) throw new ArgumentNullException(nameof(host));

            Host = host;
            Username = username;
            Password = password;
            ConnectionType = connectionType;
        }

        public ConnectionInfo(string host, int? port, string username, string password, ConnectionType connectionType = ConnectionType.FTP) : this(host, username, password, connectionType)
        {
            Port = port;
        }

        public ConnectionInfo(string host, string username, SecureString password, ConnectionType connectionType = ConnectionType.FTP)
        {
            if (string.IsNullOrEmpty(host)) throw new ArgumentNullException(nameof(host));
            if (password == null) throw new ArgumentNullException(nameof(password));

            Host = host;
            Username = username;
            SecurePassword = password;
            ConnectionType = connectionType;
        }

        public ConnectionInfo(string host, int? port, string username, SecureString password, ConnectionType connectionType = ConnectionType.FTP) : this(host, username, password, connectionType)
        {
            Port = port;
        }
        #endregion
    }
}
