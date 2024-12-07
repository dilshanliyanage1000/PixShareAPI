using Amazon.DynamoDBv2.DataModel;
using System.ComponentModel.Design;

namespace PixshareAPI.Models
{

    [DynamoDBTable("post-tbl")]
    public class Post
    {
        [DynamoDBHashKey]
        public string? PostId { get; set; }

        [DynamoDBProperty]
        public string? UserId { get; set; }

        [DynamoDBProperty]
        public string? PostCaption { get; set; }

        [DynamoDBProperty]
        public string? Location { get; set; }

        [DynamoDBProperty]
        public DateTime? PostedDate { get; set; }

        [DynamoDBProperty]
        public string? S3Url { get; set; }

        [DynamoDBProperty]
        public List<Like>? Likes { get; set; } = [];

        [DynamoDBProperty]
        public List<Comment>? Comments { get; set; } = [];

        public Post()
        {
            PostedDate = DateTime.Now;
        }
    }
    public class Comment
    {
        public string? CommentId { get; set; }
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Content { get; set; }
        public DateTime? PostedAt { get; set; }

        public Comment()
        {
            CommentId = Guid.NewGuid().ToString();
            PostedAt = DateTime.Now;
        }
    }

    public class Like
    {
        public string? LikeId { get; set; }
        public string? UserId { get; set; }

        public Like()
        {
            LikeId = Guid.NewGuid().ToString();
        }

    }
}
