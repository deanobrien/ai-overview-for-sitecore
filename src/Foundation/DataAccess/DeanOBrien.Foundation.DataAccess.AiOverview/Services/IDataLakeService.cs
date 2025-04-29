using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Management.DataLake.Store.Models;


namespace DeanOBrien.Foundation.DataAccess.AiOverview.Services
{
    public interface IDataLakeService
    {
        void uploadFileFromStream(string destinationPath, byte[] data, Dictionary<string, string> metadata = null);
        void uploadFileFromFileSystem(string sourcePath, string destinationPath);
        Stream downloadFileToStream(string sourcePath);
        bool CheckFolder(string sourcePath);
        List<string> listFilesNames(string sourcePath);
        void UpdateConnection(string accountName, string fileSystemName, string accountKey);
    }
}
