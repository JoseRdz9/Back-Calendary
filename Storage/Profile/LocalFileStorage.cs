using Supabase;

public class SupabaseFileStorage : IFileStorage
{
    private readonly Supabase.Client _supabase;
    private readonly string _supabaseUrl;

    public SupabaseFileStorage(Supabase.Client supabase, IConfiguration configuration)
    {
        _supabase = supabase;
        _supabaseUrl = configuration["Supabase:Url"]!;
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var bytes = memoryStream.ToArray();

        await _supabase
            .Storage
            .From("profilephotos")
            .Upload(bytes, fileName, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = true
            });

        // 🔥 RETORNAR SOLO EL NOMBRE DEL ARCHIVO
        return fileName;
    }

    public async Task DeleteFileAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return;

        try
        {
            await _supabase
                .Storage
                .From("profilephotos")
                .Remove(new List<string> { fileName });
        }
        catch (Exception ex)
        {
            // Log pero no fallar si no se puede eliminar
            Console.WriteLine($"⚠️ Error eliminando archivo: {ex.Message}");
        }
    }
}