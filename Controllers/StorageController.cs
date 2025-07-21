using Microsoft.AspNetCore.Mvc;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Azure.Data.Tables;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;

namespace IoTDeviceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StorageController : ControllerBase
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient;
        private readonly TableServiceClient _tableServiceClient;
        private readonly ShareServiceClient _shareServiceClient;

        public StorageController(
            BlobServiceClient blobServiceClient,
            QueueServiceClient queueServiceClient,
            TableServiceClient tableServiceClient,
            ShareServiceClient shareServiceClient)
        {
            _blobServiceClient = blobServiceClient;
            _queueServiceClient = queueServiceClient;
            _tableServiceClient = tableServiceClient;
            _shareServiceClient = shareServiceClient;
        }

        // Blob Storage
        [HttpGet("blob")]
        [SwaggerOperation(Summary = "Get list of blobs", Description = "Retrieves a list of blobs from the specified container.")]
        [SwaggerResponse(200, "List of blob names")]
        public async Task<IActionResult> GetBlobs()
        {
            var container = _blobServiceClient.GetBlobContainerClient("sample-container");
            var blobs = container.GetBlobsAsync();
            var blobNames = new List<string>();

            await foreach (BlobItem blob in blobs)
            {
                blobNames.Add(blob.Name);
            }

            return Ok(blobNames);
        }

        [HttpPost("blob")]
        [SwaggerOperation(Summary = "Add a new blob", Description = "Uploads a new blob to the specified container.")]
        [SwaggerResponse(200, "Blob uploaded successfully")]
        public async Task<IActionResult> AddBlob([FromBody] BlobRequest blob)
        {
            var container = _blobServiceClient.GetBlobContainerClient("sample-container");
            await container.CreateIfNotExistsAsync();
            var blobClient = container.GetBlobClient(blob.Name);
            var content = Encoding.UTF8.GetBytes("Sample blob content");
            using var stream = new MemoryStream(content);
            await blobClient.UploadAsync(stream, overwrite: true);
            return Ok(new { message = $"Blob '{blob.Name}' uploaded." });
        }

        // Queue Storage
        [HttpGet("queue")]
        [SwaggerOperation(Summary = "Get list of messages from queue", Description = "Retrieves a list of messages from the specified queue.")]
        [SwaggerResponse(200, "List of messages")]
        public async Task<IActionResult> GetQueues()
        {
            var queueClient = _queueServiceClient.GetQueueClient("sample-queue");
            await queueClient.CreateIfNotExistsAsync();
            var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 5);
            var messageList = messages.Value.Select(m => m.MessageText).ToList();
            return Ok(messageList);
        }

        [HttpPost("queue")]
        [SwaggerOperation(Summary = "Add a new message to queue", Description = "Adds a new message to the specified queue.")]
        [SwaggerResponse(200, "Message added to queue successfully")]
        public async Task<IActionResult> AddQueue([FromBody] QueueRequest queue)
        {
            var queueClient = _queueServiceClient.GetQueueClient("sample-queue");
            await queueClient.CreateIfNotExistsAsync();
            await queueClient.SendMessageAsync(queue.Name);
            return Ok(new { message = $"Message '{queue.Name}' added to queue." });
        }

        // Table Storage
        [HttpGet("table")]
        [SwaggerOperation(Summary = "Get list of entities from table", Description = "Retrieves a list of entities from the specified table.")]
        [SwaggerResponse(200, "List of entities")]
        public async Task<IActionResult> GetTables()
        {
            var tableClient = _tableServiceClient.GetTableClient("sampletable");
            await tableClient.CreateIfNotExistsAsync();
            var entities = tableClient.QueryAsync<TableEntity>();
            var results = new List<object>();

            await foreach (var entity in entities)
            {
                results.Add(new { entity.PartitionKey, entity.RowKey });
            }

            return Ok(results);
        }

        [HttpPost("table")]
        [SwaggerOperation(Summary = "Add a new entity to table", Description = "Adds a new entity to the specified table.")]
        [SwaggerResponse(200, "Entity added to table successfully")]
        public async Task<IActionResult> AddTable([FromBody] TableRequest table)
        {
            var tableClient = _tableServiceClient.GetTableClient("sampletable");
            await tableClient.CreateIfNotExistsAsync();
            var entity = new TableEntity("partition1", table.Name)
            {
                { "CreatedOn", DateTime.UtcNow }
            };
            await tableClient.AddEntityAsync(entity);
            return Ok(new { message = $"Entity '{table.Name}' added to table." });
        }

        // File Share Storage
        [HttpGet("fileShare")]
        [SwaggerOperation(Summary = "Get list of files from file share", Description = "Retrieves a list of files from the specified file share.")]
        [SwaggerResponse(200, "List of files")]
        public async Task<IActionResult> GetFileShares()
        {
            var shareClient = _shareServiceClient.GetShareClient("sampleshare");
            await shareClient.CreateIfNotExistsAsync();
            var rootDir = shareClient.GetRootDirectoryClient();
            var files = rootDir.GetFilesAndDirectoriesAsync();
            var fileList = new List<string>();

            await foreach (var item in files)
            {
                fileList.Add(item.Name);
            }

            return Ok(fileList);
        }

        [HttpPost("fileShare")]
        [SwaggerOperation(Summary = "Add a new file to file share", Description = "Uploads a new file to the specified file share.")]
        [SwaggerResponse(200, "File uploaded to file share successfully")]
        public async Task<IActionResult> AddFileShare([FromBody] FileShareRequest fileShare)
        {
            var shareClient = _shareServiceClient.GetShareClient("sampleshare");
            await shareClient.CreateIfNotExistsAsync();
            var rootDir = shareClient.GetRootDirectoryClient();
            var fileClient = rootDir.GetFileClient(fileShare.Name);
            byte[] content = Encoding.UTF8.GetBytes("Sample file content");
            using var stream = new MemoryStream(content);
            await fileClient.CreateAsync(content.Length);
            await fileClient.UploadRangeAsync(new HttpRange(0, content.Length), stream);
            return Ok(new { message = $"File '{fileShare.Name}' uploaded to file share." });
        }
    }

    // Renamed models to avoid conflicts
    public class BlobRequest
    {
        public string Name { get; set; }
    }

    public class QueueRequest
    {
        public string Name { get; set; }
    }

    public class TableRequest
    {
        public string Name { get; set; }
    }

    public class FileShareRequest
    {
        public string Name { get; set; }
    }
}
