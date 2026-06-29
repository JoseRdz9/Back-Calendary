public interface IFileStorage
{
    Task<string> SaveFileAsync(IFormFile file);
}