using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.S3;
using PixshareAPI.Interface;
using PixshareAPI.Repository;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

//builder.Services.AddSwaggerGen();

builder.Configuration.AddSystemsManager("/MovieDb",
    new Amazon.Extensions.NETCore.Setup.AWSOptions { Region = Amazon.RegionEndpoint.USEast1 });

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

// Add AWS services
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions("AWS"));
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPostRepository, PostRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseCors("ReactPolicy");

//app.UseSwagger();

//app.UseSwaggerUI();

app.UseAuthorization();

app.MapControllers();

app.Run();
