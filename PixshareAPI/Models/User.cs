using Amazon.DynamoDBv2.DataModel;

namespace PixshareAPI.Models
{
    [DynamoDBTable("user-tbl")]
    public partial class User
    {
        [DynamoDBHashKey]
        public string UserId { get; set; }

        [DynamoDBProperty]
        public string FullName { get; set; } = null!;

        [DynamoDBProperty]
        public string EmailAddress { get; set; } = null!;

        [DynamoDBProperty]
        public string Username { get; set; } = null!;

        [DynamoDBProperty]
        public string UserPassword { get; set; } = null!;

        public User()
        {
            UserId = Guid.NewGuid().ToString();
        }
    }
}
