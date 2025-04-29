using Azure;
using Azure.Storage;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;
using Microsoft.Azure.Management.DataLake.Store.Models;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Nexus.Consumption;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace DeanOBrien.Foundation.DataAccess.AiOverview.Services
{
    public class DataLakeService : IDataLakeService
    {
        private Database _master;
        private string _accountName;
        private string _fileSystemName;
        private string _accountKey;
        private DataLakeServiceClient _serviceClient;
        private DataLakeFileSystemClient _fileSystemClient;

        public DataLakeService() { 

            _master = Sitecore.Configuration.Factory.GetDatabase("master");

            var settingsItem = _master.GetItem("/sitecore/system/Modules/AI Language Assistant");
            if (settingsItem == null)
            {
                Log.Info("PROBLEM: DataLakeService - Settings item is missing", this);
                return;
            }

            string dataSource = string.Empty;
            if (string.IsNullOrWhiteSpace(settingsItem["Default Data Source"]))
            {
                Log.Info("PROBLEM: DataLakeService - Default data source not defined", this);
                return;
            }
            dataSource = settingsItem["Default Data Source"];

            var dataSources = _master.GetItem("/sitecore/system/Modules/AI Language Assistant/Data Sources");
            var dataSourceItem = dataSources.Children.Where(x => x.DisplayName == dataSource).FirstOrDefault();
            if (dataSourceItem == null) dataSourceItem = dataSources.Children.FirstOrDefault();
            if (dataSourceItem == null)
            {
                Log.Info("PROBLEM: DataLakeService - No data source item", this);
                return;
            }

            _accountName = dataSourceItem["Storage Account Name"];
            _accountKey = dataSourceItem["Storage Account Key"];
            _fileSystemName = dataSourceItem["Storage File System Name"];

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            _serviceClient = new DataLakeServiceClient(new Uri($"https://{_accountName}.dfs.core.windows.net"), new StorageSharedKeyCredential(_accountName, _accountKey));
            _fileSystemClient = _serviceClient.GetFileSystemClient(_fileSystemName);
        }
        public void UpdateConnection(string accountName, string fileSystemName, string accountKey)
        {
            if (_accountName == accountName && _fileSystemName == fileSystemName && _accountKey == accountKey) return;

            _accountName = accountName;

            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            _fileSystemName = fileSystemName;
            _accountKey = accountKey;

            _serviceClient = new DataLakeServiceClient(new Uri($"https://{_accountName}.dfs.core.windows.net"), new StorageSharedKeyCredential(_accountName, _accountKey));
            _fileSystemClient = _serviceClient.GetFileSystemClient(_fileSystemName);

        }

        public bool CheckFolder(string sourcePath)
        {
            DataLakeDirectoryClient directoryClient = _fileSystemClient.GetDirectoryClient(sourcePath);
            return directoryClient.Exists();
        }

        public Stream downloadFileToStream(string sourcePath)
        {
            int pos = sourcePath.LastIndexOf("/") + 1;
            var directoryName = sourcePath.Substring(0, pos);
            var fileName = sourcePath.Substring(pos, sourcePath.Length - pos);

            try
            {
                DataLakeDirectoryClient directoryClient = _fileSystemClient.GetDirectoryClient(directoryName);

                DataLakeFileClient fileClient = directoryClient.GetFileClient(fileName);

                Response<FileDownloadInfo> downloadResponse = fileClient.Read();
                return downloadResponse.Value.Content;

            }
            catch
            {
                Sitecore.Diagnostics.Log.Info($"Download failed: {directoryName}/{fileName}", this);
            }
            return null;
        }

        public List<string> listFilesNames(string sourcePath)
        {
            try
            {
                DataLakeDirectoryClient directoryClient = _fileSystemClient.GetDirectoryClient(sourcePath);
                return directoryClient.GetPaths().Select(x => x.Name).ToList();
            }
            catch
            {
                return null;
            }

        }

        public void uploadFileFromFileSystem(string sourcePath, string destinationPath)
        {
            int pos = destinationPath.LastIndexOf("/") + 1;
            var directoryName = destinationPath.Substring(0, pos);
            var fileName = destinationPath.Substring(pos, destinationPath.Length - pos);

            try
            {
                DataLakeDirectoryClient directoryClient = _fileSystemClient.GetDirectoryClient(directoryName);
                directoryClient.CreateIfNotExists();

                DataLakeFileClient fileClient = directoryClient.GetFileClient(fileName);
                fileClient.CreateIfNotExists();

                var response = fileClient.Upload(sourcePath, true);

                if (response != null)
                {
                    Log.Info($"Response from upload call for destination {directoryName}/{fileName}", this);
                }
                else
                {
                    Log.Info($"No Response from upload call for destination {directoryName}/{fileName}", this);
                }

            }
            catch
            {
                Log.Info($"Upload failed: {directoryName}/{fileName}", this);
            }
        }

        public void uploadFileFromStream(string destinationPath, byte[] data, Dictionary<string, string> metadata = null)
        {
            if (metadata == null) metadata = new Dictionary<string, string>();
            int pos = destinationPath.LastIndexOf("/") + 1;
            var directoryName = destinationPath.Substring(0, pos);
            var fileName = destinationPath.Substring(pos, destinationPath.Length - pos);

            try
            {
                DataLakeDirectoryClient directoryClient = _fileSystemClient.GetDirectoryClient(directoryName);
                directoryClient.CreateIfNotExists();
                DataLakePathCreateOptions opts = new DataLakePathCreateOptions() { Metadata = metadata };
                DataLakeFileClient fileClient = directoryClient.GetFileClient(fileName);
                fileClient.CreateIfNotExists(opts);

                using (MemoryStream memStream = new MemoryStream())
                {
                    memStream.Write(data, 0, data.Length);
                    memStream.Seek(0, SeekOrigin.Begin);

                    var response = fileClient.Upload(memStream, true);

                    if (response != null)
                    {
                        Log.Info($"Response from upload call for destination {directoryName}/{fileName}", this);
                    }
                    else
                    {
                        Log.Info($"No Response from upload call for destination {directoryName}/{fileName}", this);
                    }
                }
            }
            catch(Exception ex)
            {
                Log.Info($"Upload failed: {directoryName}/{fileName} \r\n {ex.Message}", this);
            }
        }
    }
}
