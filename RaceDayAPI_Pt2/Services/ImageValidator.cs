namespace RaceDayApi.Services
{
    // checks an uploaded file really is a small image before we send it to Blob Storage
    public static class ImageValidator
    {
        private static readonly string[] AllowedTypes = { "image/jpeg", "image/png", "image/webp" };
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

        // returns an error message, or null when the file is fine
        public static string? Validate(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return "Please choose an image file.";
            }

            if (file.Length > MaxBytes)
            {
                return "The image must be 5 MB or smaller.";
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedTypes.Contains(file.ContentType.ToLowerInvariant()) || !AllowedExtensions.Contains(extension))
            {
                return "Only JPG, PNG or WEBP images are allowed.";
            }

            return null;
        }
    }
}
