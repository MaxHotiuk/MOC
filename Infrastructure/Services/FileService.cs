using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class FileService(ApplicationDbContext context) : IFileService
    {
        private readonly ApplicationDbContext _context = context;

        public async Task<int> UploadOrCreateFileAsync(DbFile dbFile)
        {
            _context.Files.Add(dbFile);
            await _context.SaveChangesAsync();
            return dbFile.Id;
        }

        public async Task<List<DbFile>> GetUserFilesAsync(string userId)
        {
            return await _context.Files
                .Where(f => f.UserId == userId)
                .ToListAsync();
        }

        public async Task DeleteFileAsync(int fileId)
        {
            var file = await _context.Files.FindAsync(fileId);
            if (file != null)
            {
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<DbFile> GetFileByIdAsync(int fileId, string userId)
        {
            return await _context.Files
                .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId) ?? throw new KeyNotFoundException();
        }
    }
}