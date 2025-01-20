using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace fnPostDataStorage
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("dataStorage")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("Processando a imagem no Storage.");

            if (!req.Headers.TryGetValue("file-type", out var fileTypeHeader))
            {
                return new BadRequestObjectResult("O cabeçalho 'file-type' é obrigatório");
            }

            var fileType = fileTypeHeader.ToString();
            var form = await req.ReadFormAsync();
            var file = req.Form.Files["file"];

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("O arquivo não foi enviado");
            }

            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string containerName = fileType;
            BlobContainerClient containerClient = new BlobContainerClient(connectionString, containerName);
            BlobClient blobClient = containerClient.GetBlobClient(file.FileName);

            // Certifique-se de que o contêiner existe
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Subir o arquivo para o Blob Storage
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, true);
            }

            _logger.LogInformation($"Arquivo {file.FileName} armazenado com sucesso.");

            return new OkObjectResult(new
            {
                Message = "Arquivo armazenado com sucesso",
                BlobUri = blobClient.Uri
            });
        }
    }
}
