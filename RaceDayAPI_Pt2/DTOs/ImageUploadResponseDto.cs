namespace RaceDayApi.DTOs
{
    // sent back after an image upload, Url is the public link to the image in Blob Storage
    public class ImageUploadResponseDto
    {
        public string Url { get; set; } = string.Empty;
    }
}
