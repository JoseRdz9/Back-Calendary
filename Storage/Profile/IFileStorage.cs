public interface IFileStorage
{
    Task<string> SaveFileAsync(IFormFile file);
    Task DeleteFileAsync(string fileName);
}