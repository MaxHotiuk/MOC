using Core.Entities;

namespace Core.Interfaces
{
    public interface IFileService
    {
        Task<int> UploadOrCreateFileAsync(DbFile dbFile);
        Task<List<DbFile>> GetUserFilesAsync(string userId);
        Task DeleteFileAsync(int fileId);
        Task<DbFile> GetFileByIdAsync(int fileId, string userId);
    }
}