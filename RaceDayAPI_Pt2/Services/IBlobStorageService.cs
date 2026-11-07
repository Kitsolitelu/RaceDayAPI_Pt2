namespace RaceDayApi.Services
{
    // the API talks to Azure Blob Storage only through this, the MVC app never does
    public interface IBlobStorageService
    {
        // saves the image and returns the link to it
        Task<string> UploadImageAsync(IFormFile file, string folder);
    }
}
