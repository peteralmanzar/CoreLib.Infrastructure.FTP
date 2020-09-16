using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CoreLib.Infrastructure.FTP
{
    public static class FTPClient
    {
        #region Fields
        private static readonly string _ftpScheme = "ftp";
        private static readonly int _defaultFTPPort = 21;
        #endregion

        #region Public Methods
        /// <summary>
        /// List files and directories under specified path on FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <returns>Array of strings that contain the names of all the files and directories.</returns>
        public static async Task<List<string>> ListDirectory(ConnectionInfo connectionInfo)
        {
            if (connectionInfo is null)
                throw new ArgumentNullException(nameof(connectionInfo));

            var result = new List<string>();
            if (connectionInfo.ConnectionType == ConnectionType.FTP) result = await ftp_ListDirectory(connectionInfo);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) result = await Task.Run(() => sftp_ListDirectory(connectionInfo));

            return result;
        }

        /// <summary>
        /// Upload file or directory to specified FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="sourcePath">Path to file or directory to be uploaded.</param>
        /// <returns></returns>
        public static async Task Upload(ConnectionInfo connectionInfo, string sourcePath, string destinationName = "")
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if (string.IsNullOrEmpty(destinationName)) destinationName = Path.GetFileName(sourcePath);

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_Upload(connectionInfo, sourcePath, destinationName);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await Task.Run(() => sftp_Upload(connectionInfo, sourcePath, destinationName));
        }

        /// <summary>
        /// Download file or directory from FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="sourceName">Name of the file or directory to be downloaded.</param>
        /// <param name="destinationPath">Path to file or directory to be downloaded.</param>
        /// <param name="destinationName">Name for the downloading file or directory.</param>
        /// <returns></returns>
        public static async Task Download(ConnectionInfo connectionInfo, string sourceName, string destinationPath, string destinationName = "")
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceName)) throw new ArgumentNullException(nameof(sourceName));
            if (string.IsNullOrEmpty(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
            if (string.IsNullOrEmpty(destinationName)) destinationName = sourceName;

            var destinationFullPath = Path.Combine(destinationPath, destinationName);

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_Download(connectionInfo, sourceName, destinationFullPath);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await Task.Run(() => sftp_Download(connectionInfo, sourceName, destinationFullPath));
        }

        /// <summary>
        /// Delete file from FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="destinationName">Name of file on FTP server.</param>
        /// <returns></returns>
        public static async Task DeleteFile(ConnectionInfo connectionInfo, string destinationName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(destinationName)) throw new ArgumentNullException(nameof(destinationName));

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_DeleteFile(connectionInfo, destinationName);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await sftp_DeleteFile(connectionInfo, destinationName);
        }

        /// <summary>
        /// Rename file or directory on the FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="destinationName">Name of file or directory on the FTP server.</param>
        /// <param name="destinationNewName">New name for the specified file or directory on the FTP server.</param>
        /// <returns></returns>
        public static async Task RenameFile(ConnectionInfo connectionInfo, string destinationName, string destinationNewName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(destinationName)) throw new ArgumentNullException(nameof(destinationName));
            if (string.IsNullOrEmpty(destinationNewName)) throw new ArgumentNullException(nameof(destinationNewName));

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_Rename(connectionInfo, destinationName, destinationNewName);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await sftp_Rename(connectionInfo, destinationName, destinationNewName);
        }

        /// <summary>
        /// Create new directory on FTP server.
        /// </summary>
        /// <param name="connectionInfo">The connection infomation.</param>
        /// <param name="directoryName">Name of the the new directory.</param>
        /// <returns></returns>
        public static async Task MakeDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentNullException(nameof(directoryName));

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_MakeDirectory(connectionInfo, directoryName);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await sftp_MakeDirectory(connectionInfo, directoryName);
        }

        /// <summary>
        /// Removes directory from specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="directory">Name of directory to be deleted.</param>
        /// <returns></returns>
        public static async Task RemoveDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentNullException(nameof(directoryName));

            if (connectionInfo.ConnectionType == ConnectionType.FTP) await ftp_RemoveDirectory(connectionInfo, directoryName);
            else if (connectionInfo.ConnectionType == ConnectionType.SFTP) await sftp_RemoveDirectory(connectionInfo, directoryName);
        }
        #endregion

        #region Private Methods
        #region Logic: FTP
        private static async Task<List<string>> ftp_ListDirectory(ConnectionInfo connectionInfo)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));

            var ftpRequest = getFTPWebRequest(connectionInfo, null, WebRequestMethods.Ftp.ListDirectory);

            using (var response = await ftpRequest.GetResponseAsync())
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var result = new List<string>();
                var streamLine = string.Empty;

                do
                {
                    streamLine = await streamReader.ReadLineAsync();
                    result.Add(streamLine);
                } while (!string.IsNullOrEmpty(streamLine));

                return result;
            }
        }
        private static async Task ftp_Upload(ConnectionInfo connectionInfo, string sourcePath, string destinationName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));

            var pathType = getLocalPathType(sourcePath);
            if (pathType == PathType.File) await ftp_UploadFile(connectionInfo, sourcePath, destinationName);
            else if (pathType == PathType.Directory) ftp_UploadDirectory(connectionInfo, sourcePath, destinationName);
        }
        private static async Task ftp_UploadFile(ConnectionInfo connectionInfo, string filePath, string destinationName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (getLocalPathType(filePath) != PathType.File) throw new Exception($"'{filePath}' is not a valid file path.");

            var ftpRequest = (FtpWebRequest)getFTPWebRequest(connectionInfo, WebRequestMethods.Ftp.UploadFile);
            ftpRequest.RenameTo= destinationName;

            using (var response = await ftpRequest.GetResponseAsync())
            using (Stream ftpStream = response.GetResponseStream(),
                         fileStream = File.OpenRead(filePath))
            {
                var fileBytes = fileStream.toBytes();                
                await ftpStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }
        }
        private static void ftp_UploadDirectory(ConnectionInfo connectionInfo, string directoryPath, string destinationFullFilePath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (getLocalPathType(directoryPath) != PathType.Directory) throw new Exception($"'{directoryPath}' is not a valid directory path.");

            //create directory
            //copy each file in directory
            //get list of directories
            //call upload directories per directory
            throw new NotImplementedException($"Unable to upload directory.");
        }
        private static async Task ftp_Download(ConnectionInfo connectionInfo, string sourceName, string destinationFullName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceName)) throw new ArgumentException(nameof(sourceName));
            if (string.IsNullOrEmpty(destinationFullName)) throw new ArgumentException(nameof(destinationFullName));

            var pathType = getLocalPathType(destinationFullName);
            if (pathType == PathType.File) await ftp_DownloadFile(connectionInfo, sourceName, destinationFullName);
            else if (pathType == PathType.Directory) ftp_DownloadDirectory(connectionInfo, sourceName, destinationFullName);
        }
        private static async Task ftp_DownloadFile(ConnectionInfo connectionInfo, string sourceFileName, string destinationFullFilePath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceFileName)) throw new ArgumentException(nameof(sourceFileName));
            if (string.IsNullOrEmpty(destinationFullFilePath)) throw new ArgumentException(nameof(destinationFullFilePath));

            var ftpRequest = (FtpWebRequest)getFTPWebRequest(connectionInfo, sourceFileName, WebRequestMethods.Ftp.DownloadFile);

            using (var response = await ftpRequest.GetResponseAsync())
            using (var ftpStream = response.GetResponseStream())
                await Task.Run(() =>
                {
                    File.WriteAllBytes(destinationFullFilePath, ftpStream.toBytes());
                });
        }
        private static void ftp_DownloadDirectory(ConnectionInfo connectionInfo, string sourceDirectoryName, string destinationFullDirectoryPath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceDirectoryName)) throw new ArgumentException(nameof(sourceDirectoryName));
            if (string.IsNullOrEmpty(destinationFullDirectoryPath)) throw new ArgumentException(nameof(destinationFullDirectoryPath));

            //var ftpRequest = getFTPWebRequest(connectionInfo, sourceFileName, WebRequestMethods.Ftp.DownloadFile);

            //create directory
            //copy each file in directory
            //get list of directories
            //call download directories per directory
            throw new NotImplementedException($"Unable to download directory.");
        }
        private static async Task ftp_DeleteFile(ConnectionInfo connectionInfo, string fileName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(fileName)) throw new ArgumentException(nameof(fileName));

            var ftpRequest = getFTPWebRequest(connectionInfo, fileName, WebRequestMethods.Ftp.DeleteFile);
            var response = await ftpRequest.GetResponseAsync();
            response.Dispose();
        }
        private static async Task ftp_Rename(ConnectionInfo connectionInfo, string destinationFileName, string destinationNewFileName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(destinationFileName)) throw new ArgumentException(nameof(destinationFileName));
            if (string.IsNullOrEmpty(destinationNewFileName)) throw new ArgumentException(nameof(destinationNewFileName));

            var ftpRequest = (FtpWebRequest)getFTPWebRequest(connectionInfo, destinationFileName, WebRequestMethods.Ftp.Rename);
            ftpRequest.RenameTo = getFTPWebRequest(connectionInfo, destinationNewFileName).RequestUri.ToString();
            var response = await ftpRequest.GetResponseAsync();
            response.Dispose();
        }
        private static async Task ftp_MakeDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentException(nameof(directoryName));

            var ftpRequest = getFTPWebRequest(connectionInfo, directoryName, WebRequestMethods.Ftp.MakeDirectory);
            var response = await ftpRequest.GetResponseAsync();
            response.Dispose();
        }
        private static async Task ftp_RemoveDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentException(nameof(directoryName));

            var ftpRequest = getFTPWebRequest(connectionInfo, directoryName, WebRequestMethods.Ftp.RemoveDirectory);
            var response = await ftpRequest.GetResponseAsync();
            response.Dispose();
        }

        private static PathType ftp_GetPathType(ConnectionInfo connectionInfo, string name)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(nameof(name));

            var ftpRequest = getFTPWebRequest(connectionInfo, name, WebRequestMethods.Ftp.ListDirectory);
            try
            {
                ftpRequest.GetResponse();
                return PathType.Directory;
            }
            catch (WebException e)
            {
                using (var response = (FtpWebResponse)e.Response)
                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                        return PathType.File;
                    else return PathType.Directory;
            }
        }
        #endregion

        #region Logic: SFTP
        private static List<string> sftp_ListDirectory(ConnectionInfo connectionInfo)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));

            var result = new List<string>();

            using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
            {
                sftp.Connect();
                sftp.ChangeDirectory(connectionInfo.Path);
                result = sftp.ListDirectory(connectionInfo.Path).Select(f => f.Name).ToList();
            }

            return result;
        }
        private static async Task sftp_Upload(ConnectionInfo connectionInfo, string sourcePath, string destinationName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if (string.IsNullOrEmpty(destinationName)) throw new ArgumentNullException(nameof(destinationName));

            var pathType = getLocalPathType(sourcePath);
            if (pathType == PathType.File) await sftp_UploadFile(connectionInfo, sourcePath, destinationName);
            else if (pathType == PathType.Directory) await sftp_UploadDirectory(connectionInfo, sourcePath, destinationName);
        }
        private static async Task sftp_UploadFile(ConnectionInfo connectionInfo, string sourceFilePath, string destinationFileName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceFilePath)) throw new ArgumentNullException(nameof(sourceFilePath));
            if (string.IsNullOrEmpty(destinationFileName)) throw new ArgumentNullException(nameof(destinationFileName));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    using (var fileStream = File.OpenRead(sourceFilePath))
                        sftp.UploadFile(fileStream, destinationFileName);
                }
            });
        }
        private static async Task sftp_UploadDirectory(ConnectionInfo connectionInfo, string sourceDirectoryPath, string destinationDirectoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceDirectoryPath)) throw new ArgumentNullException(nameof(sourceDirectoryPath));
            if (string.IsNullOrEmpty(destinationDirectoryName)) throw new ArgumentNullException(nameof(destinationDirectoryName));

            //create directory
            //copy each file in directory
            //get list of directories
            //call upload directories per directory
            await Task.Run(() => {
                throw new NotImplementedException($"Unable to upload directory.");
            });
        }
        private static async Task sftp_Download(ConnectionInfo connectionInfo, string sourceName, string destinationFullPath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceName)) throw new ArgumentNullException(nameof(sourceName));
            if (string.IsNullOrEmpty(destinationFullPath)) throw new ArgumentException(nameof(destinationFullPath));

            var pathType = await sftp_GetPathType(connectionInfo, sourceName);
            if (pathType == PathType.File) await sftp_DownloadFile(connectionInfo, sourceName, destinationFullPath);
            else if (pathType == PathType.Directory) await sftp_DownloadDirectory(connectionInfo, sourceName, destinationFullPath);
        }
        private static async Task sftp_DownloadFile(ConnectionInfo connectionInfo, string sourceFileName, string destinationFullFilePath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceFileName)) throw new ArgumentNullException(nameof(sourceFileName));
            if (string.IsNullOrEmpty(destinationFullFilePath)) throw new ArgumentNullException(nameof(destinationFullFilePath));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);

                    using (var fileStream = File.OpenWrite(destinationFullFilePath))
                        sftp.DownloadFile(sourceFileName, fileStream);
                }
            });
        }
        private static async Task sftp_DownloadDirectory(ConnectionInfo connectionInfo, string sourceDirectoryName, string destinationFullDirectoryPath)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourceDirectoryName)) throw new ArgumentException(nameof(sourceDirectoryName));
            if (string.IsNullOrEmpty(destinationFullDirectoryPath)) throw new ArgumentNullException(nameof(destinationFullDirectoryPath));

            //create directory
            //copy each file in directory
            //get list of directories
            //call download directories per directory
            await Task.Run(() => {
                throw new NotImplementedException($"Unable to download directory.");
            });
        }
        private static async Task sftp_DeleteFile(ConnectionInfo connectionInfo, string name)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    sftp.DeleteFile(name);
                }
            });
        }
        private static async Task sftp_Rename(ConnectionInfo connectionInfo, string destinationName, string destinationNewName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(destinationName)) throw new ArgumentException(nameof(destinationName));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    sftp.RenameFile(destinationName, destinationNewName);
                }
            });
        }
        private static async Task sftp_MakeDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentException(nameof(directoryName));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    sftp.CreateDirectory(directoryName);
                }
            });
        }
        private static async Task sftp_RemoveDirectory(ConnectionInfo connectionInfo, string directoryName)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(directoryName)) throw new ArgumentException(nameof(directoryName));

            await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    sftp.DeleteDirectory(directoryName);
                }
            });
        }

        private static async Task<PathType> sftp_GetPathType(ConnectionInfo connectionInfo, string name)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(name)) throw new ArgumentException(nameof(name));

            return await Task.Run(() => {
                using (var sftp = new SftpClient(connectionInfo.ToSshNetConnectionInfo()))
                {
                    sftp.Connect();
                    sftp.ChangeDirectory(connectionInfo.Path);
                    if (sftp.GetAttributes(name).IsDirectory) return PathType.Directory;
                    else return PathType.File;
                }
            });
        }
        #endregion

        private static WebRequest getFTPWebRequest(ConnectionInfo connectionInfo, string sourcePath = null, string method = null)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));
            if (string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));

            var uri = getFTPUri(connectionInfo);
            if (!string.IsNullOrEmpty(sourcePath)) uri = getFTPFileUri(uri, Path.GetFileName(sourcePath));

            var result = WebRequest.Create(uri);
            result.Credentials = new NetworkCredential(connectionInfo.Username, connectionInfo.SecurePassword);

            if (!string.IsNullOrEmpty(method))
                result.Method = method;

            return result;
        }
        private static Uri getFTPUri(ConnectionInfo connectionInfo)
        {
            if (connectionInfo is null) throw new ArgumentNullException(nameof(connectionInfo));

            Uri result;
            if (connectionInfo.Port is null || connectionInfo.Port == 0)
                result = new UriBuilder()
                {
                    Scheme = _ftpScheme,
                    Host = connectionInfo.Host,
                    Path = connectionInfo.Path ?? string.Empty
                }.Uri;
            else
                result = new UriBuilder()
                {
                    Scheme = _ftpScheme,
                    Host = connectionInfo.Host,
                    Port = connectionInfo.Port ?? _defaultFTPPort,
                    Path = connectionInfo.Path ?? string.Empty
                }.Uri;

            return result;
        }

        private static Uri getFTPFileUri(Uri uri, string fileName)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));
            if (fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            var ftpFileUriString = Path.Combine(uri.ToString(), fileName);
            return new UriBuilder(ftpFileUriString).Uri;
        }
        private static PathType getLocalPathType(string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory) ? PathType.Directory : PathType.File;
        private static byte[] toBytes(this Stream inputStream)
        {
            if (inputStream is null)
                throw new ArgumentNullException(nameof(inputStream));

            using (var memoryStream = new MemoryStream())
            {
                inputStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        private static Renci.SshNet.ConnectionInfo ToSshNetConnectionInfo(this ConnectionInfo connectionInfo) => new Renci.SshNet.ConnectionInfo(connectionInfo.Host, 
            connectionInfo.Port ?? _defaultFTPPort, 
            connectionInfo.Username, 
            new PasswordAuthenticationMethod(connectionInfo.Username, connectionInfo.Password));
        #endregion


    }
}
