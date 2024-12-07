using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2;
using Amazon.S3;
using PixshareAPI.Interface;
using PixshareAPI.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add AWS services
var awsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
{
    Credentials = new Amazon.Runtime.BasicAWSCredentials("AKIAQR5EPKDC5EPNSWXD", "URm1dFYp2UB9jH9r94ln/xf4ML0/eEB9cNVmap2u"),
    Region = Amazon.RegionEndpoint.USEast1
};

builder.Services.AddDefaultAWSOptions(awsOptions);

builder.Services.AddAWSService<IAmazonDynamoDB>();

builder.Services.AddAWSService<IAmazonS3>();

builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

builder.Services.AddDistributedMemoryCache();

builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPostRepository, PostRepository>();

builder.Services.AddScoped<ILikeRepository, LikeRepository>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseCors("ReactPolicy");

app.UseAuthorization();

app.MapControllers();

app.Run();
