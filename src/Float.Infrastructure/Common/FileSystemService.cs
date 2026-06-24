using System;
using System.IO;
using Float.Core.Abstractions.Services;

namespace Float.Infrastructure.Common;

public class FileSystemService : IFileSystemService
{
    public string GetFloatTempDir()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".float", "tmp");
        Directory.CreateDirectory(dir);
        return dir;
    }
}
