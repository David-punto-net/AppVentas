﻿using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AppVentasWeb.Helper
{
    public class BlobHelper : IBlobHelper
    {
        private readonly CloudBlobClient _blobClient;

        public BlobHelper(IConfiguration configuration)
        {
            try
            {
                string keys = configuration["Blob:ConnectionString"];
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(keys);
                _blobClient = storageAccount.CreateCloudBlobClient();
            }
            catch 
            { 
            }
        }

        public async Task DeleteBlobAsync(Guid id, string containerName)
        {
            try
            {
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{id}");
                await blockBlob.DeleteAsync();
            }
            catch { }

        }
        public async Task<Guid> UploadBlobAsync(IFormFile file, string containerName)
        {
            Stream stream = file.OpenReadStream();
            return await UploadBlobAsync(stream, containerName);
        }
        public async Task<Guid> UploadBlobAsync(byte[] file, string containerName)
        {
            MemoryStream stream = new MemoryStream(file);
            return await UploadBlobAsync(stream, containerName);
        }
        public async Task<Guid> UploadBlobAsync(string image, string containerName)
        {
            Stream stream = File.OpenRead(image);
            return await UploadBlobAsync(stream, containerName);
        }
        private async Task<Guid> UploadBlobAsync(Stream stream, string containerName)
        {
            try
            {
                Guid name = Guid.NewGuid();
                CloudBlobContainer container = _blobClient.GetContainerReference(containerName);
                CloudBlockBlob blockBlob = container.GetBlockBlobReference($"{name}");
                await blockBlob.UploadFromStreamAsync(stream);
                return name;
                //return Guid.Empty;
            }
            catch (Exception ex) {
                return Guid.Empty;
            }
         
        }

    }
}
