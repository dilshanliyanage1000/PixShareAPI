namespace PixshareAPI.Models
{
    public class LikeRequest
    {
        public required string PostId { get; set; }
        public required string UserId { get; set; }
    }

}
