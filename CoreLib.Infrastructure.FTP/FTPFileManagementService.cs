using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace CoreLib.Infrastructure.FTP
{
    public static class FTPFileManagementService
    {
        #region Public Methods
        /// <summary>
        /// Lists files in specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <returns>List of strings that containt all the FTP files.</returns>
        public static async Task<List<string>> ListFiles(Uri uri, NetworkCredential credentials)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            var ftpRequest = getFTPWebRequest(uri, credentials, WebRequestMethods.Ftp.ListDirectory);
            
            using(var response = await ftpRequest.GetResponseAsync())
                using(var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = new List<string>();
                    var streamLine = string.Empty;

                    do
                    {
                        streamLine = await streamReader.ReadLineAsync();
                        result.Add(streamLine);
                    } while(!string.IsNullOrEmpty(streamLine));

                    return result;
                }
        }

        /// <summary>
        /// Uploads file to specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="uploadFilePath">File path to be uploaded.</param>
        /// <param name="fTPFileName">Name of file in the FTP server.</param>
        /// <returns></returns>
        public static async Task UploadFile(Uri uri, NetworkCredential credentials, string uploadFilePath, string fTPFileName)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(uploadFilePath))
                throw new ArgumentNullException(nameof(uploadFilePath));
            if(File.Exists(uploadFilePath))
                throw new FileNotFoundException("Unalbe to find File.", nameof(uploadFilePath));
            if(string.IsNullOrEmpty(fTPFileName))
                throw new ArgumentNullException(nameof(fTPFileName));

            var ftpFileUri = getFTPFileUri(uri, fTPFileName);
            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.UploadFile);
            
            using(var response = await ftpRequest.GetResponseAsync())
            using(Stream ftpStream = response.GetResponseStream(),
                         fileStream = File.OpenRead(uploadFilePath))
            {
                var fileBytes = fileStream.toBytes();
                await ftpStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }
        }

        /// <summary>
        /// Download file from specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="fTPFileName">Name of file in the FTP server to download.</param>
        /// <param name="downloadFilePath">Path where downloaded file will go.</param>
        /// <returns></returns>
        public static async Task DownloadFile(Uri uri, NetworkCredential credentials, string fTPFileName, string downloadFilePath)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(fTPFileName))
                throw new ArgumentNullException(nameof(fTPFileName));
            if(string.IsNullOrEmpty(downloadFilePath))
                throw new ArgumentNullException(downloadFilePath);

            var ftpFileUri = getFTPFileUri(uri, fTPFileName);
            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.DownloadFile);

            using(var response = await ftpRequest.GetResponseAsync())
            using(var ftpStream = response.GetResponseStream())
                await Task.Run(() =>
                {
                    File.WriteAllBytes(downloadFilePath, ftpStream.toBytes());
                });
        }

        /// <summary>
        /// Deletes file from specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="fTPFileName">Name of file in the FTP server to delete.</param>
        /// <returns></returns>
        public static async Task DeleteFile(Uri uri, NetworkCredential credentials, string fTPFileName)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(fTPFileName))
                throw new ArgumentNullException(nameof(fTPFileName));

            var ftpFileUri = getFTPFileUri(uri, fTPFileName);
            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.DeleteFile);

            await ftpRequest.GetResponseAsync();
        }

        /// <summary>
        /// Renames file in specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="fTPFileName">Name of file in the FTP server do rename.</param>
        /// <param name="reName">New Name for FTP file.</param>
        /// <returns></returns>
        public static async Task RenameFile(Uri uri, NetworkCredential credentials, string fTPFileName, string reName)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(fTPFileName))
                throw new ArgumentNullException(nameof(fTPFileName));
            if(string.IsNullOrEmpty(reName))
                throw new ArgumentNullException(nameof(reName));

            var ftpFileUri = getFTPFileUri(uri, fTPFileName);
            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.DownloadFile);
            ftpRequest.RenameTo = reName;

            await ftpRequest.GetResponseAsync();
        }

        /// <summary>
        /// Create a new directory in specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="directory">Name of new directory to be created.</param>
        /// <returns></returns>
        public static async Task MakeDirectory(Uri uri, NetworkCredential credentials, string directory)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            var ftpFileUriString = Path.Combine(uri.ToString(), directory);
            var ftpFileUri = new UriBuilder(ftpFileUriString).Uri;

            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.MakeDirectory);

            await ftpRequest.GetResponseAsync();
        }

        /// <summary>
        /// Removes directory from specified FTP server.
        /// </summary>
        /// <param name="uri">FTP server address.</param>
        /// <param name="credentials">Login credentials to FTP server.</param>
        /// <param name="directory">Name of directory to be deleted.</param>
        /// <returns></returns>
        public static async Task RemoveDirectory(Uri uri, NetworkCredential credentials, string directory)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(credentials is null)
                throw new ArgumentNullException(nameof(credentials));
            if(string.IsNullOrEmpty(directory))
                throw new ArgumentNullException(nameof(directory));

            var ftpFileUriString = Path.Combine(uri.ToString(), directory);
            var ftpFileUri = new UriBuilder(ftpFileUriString).Uri;

            var ftpRequest = getFTPWebRequest(ftpFileUri, credentials, WebRequestMethods.Ftp.RemoveDirectory);

            await ftpRequest.GetResponseAsync();
        }
        #endregion

        #region Private Methods
        private static FtpWebRequest getFTPWebRequest(Uri uri, NetworkCredential credentials, string method = null)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));

            var result = (FtpWebRequest)WebRequest.Create(uri);
            result.Credentials = credentials;

            if(!string.IsNullOrEmpty(method))
                result.Method = method;

            return result;
        }

        private static Uri getFTPFileUri(Uri uri, string fileName)
        {
            if(uri is null)
                throw new ArgumentNullException(nameof(uri));
            if(fileName is null)
                throw new ArgumentNullException(nameof(fileName));

            var ftpFileUriString = Path.Combine(uri.ToString(), fileName);
            return new UriBuilder(ftpFileUriString).Uri;
        }

        private static byte[] toBytes(this Stream inputStream)
        {
            if(inputStream is null)
                throw new ArgumentNullException(nameof(inputStream));

            using(var memoryStream = new MemoryStream())
            {
                inputStream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        #endregion
    }
}
