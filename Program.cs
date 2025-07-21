using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Data.Tables;
using Azure.Storage.Files.Shares;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "IoTDeviceApi", Version = "v1" });
    c.EnableAnnotations();
});

// Register Azure Storage clients
builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration["AzureStorage:Blob"]));
builder.Services.AddSingleton(new QueueServiceClient(builder.Configuration["AzureStorage:Queue"]));
builder.Services.AddSingleton(new TableServiceClient(builder.Configuration["AzureStorage:Table"]));
builder.Services.AddSingleton(new ShareServiceClient(builder.Configuration["AzureStorage:FileShare"]));


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "IoTDeviceApi v1"));
}


app.UseAuthorization();
app.MapControllers();

app.Run();
