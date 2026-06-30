using System;

namespace Float.Core.Abstractions.Services;

public interface IFileSystemService
{
    public string GetFloatTempDir();
    public string GetFloatVolumesDir();
}
