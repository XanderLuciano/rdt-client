using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using RdtClient.Data.Data;
using RdtClient.Data.Models.Data;
using RdtClient.Data.Models.DebridClient;
using RdtClient.Service.Helpers;

namespace RdtClient.Service.Test.Helpers;

public class DownloadHelperTest
{
    private static IDisposable WithMoveSingleFilesToRoot(Boolean enabled)
    {
        var previous = SettingData.Get.DownloadClient.MoveSingleFilesToRoot;
        SettingData.Get.DownloadClient.MoveSingleFilesToRoot = enabled;
        return new SettingReset(() => SettingData.Get.DownloadClient.MoveSingleFilesToRoot = previous);
    }

    private sealed class SettingReset(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenRdNameNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = null
        };

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenRdNameNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = null
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenDownloadLinkNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = null,
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenDownloadLinkNull_ReturnsNull()
    {
        // Arrange
        var download = new Download
        {
            Link = null,
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        Assert.Null(path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenDownloadFileNameNull_UsesLinkToGuessFileName()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/filename-from-link.txt",
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName, "filename-from-link.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenDownloadFileNameNull_UsesLinkToGuessFileName()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/filename-from-link.txt",
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        var expectedPath = Path.Combine(torrent.RdName, "filename-from-link.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenValid_CreatesDirectory()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedDirectoryPath = Path.Combine("/data/downloads", torrent.RdName);
        Assert.True(fileSystem.Directory.Exists(expectedDirectoryPath));
        var expectedPath = Path.Combine(expectedDirectoryPath, download.FileName);
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFileInSubdirectories_ReturnsPathWithSubdirectories()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("inside", "lots", "of", "subdirectories", "file.txt");

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName, "inside", "lots", "of", "subdirectories", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenFileInSubdirectories_ReturnsPathWithSubdirectories()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("inside", "lots", "of", "subdirectories", "file.txt");

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        var expectedPath = Path.Combine(torrent.RdName, "inside", "lots", "of", "subdirectories", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFilePathStartsWithTorrentName_StripsPrefix()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("Torrent Name", "Saison 1", "file.txt");

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        // The torrent name prefix in the file path should not duplicate the torrent name in the base dir
        var expectedPath = Path.Combine("/data/downloads", "Torrent Name", "Saison 1", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenFilePathStartsWithTorrentName_StripsPrefix()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url/file.txt",
            FileName = "file.txt"
        };

        var fileRelativePath = Path.Combine("Torrent Name", "Saison 1", "file.txt");

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = fileRelativePath
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        var expectedPath = Path.Combine("Torrent Name", "Saison 1", "file.txt");
        Assert.Equal(expectedPath, path);
    }

    // This is probably a bug
    [Fact]
    public void GetDownloadPath_WithPath_WhenNoUriSegmentsOrFileName_ReturnsTorrentDirectory()
    {
        // Arrange
        var download = new Download
        {
            Link = "https://fake.url", // HttpUtility.UrlDecode(new Uri("https://fake.url").Segments.Last()) == "/"
            FileName = null
        };

        var torrent = new Torrent
        {
            RdName = "Torrent Name"
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads", torrent.RdName);
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFlattenEnabledAndSingleFile_PlacesFileAtCategoryRoot()
    {
        // Arrange
        using var _ = WithMoveSingleFilesToRoot(true);

        var download = new Download
        {
            Link = "https://fake.url/Fixer.Upper.S05E03.mkv",
            FileName = "Fixer.Upper.S05E03.mkv"
        };

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = "Fixer.Upper.S05E03.mkv"
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Fixer.Upper.S05E03.1080p.WEB.x264",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads/tv", torrent, download, fileSystem);

        // Assert — file lands directly in the category root, no torrent-name subdirectory
        var expectedPath = Path.Combine("/data/downloads/tv", "Fixer.Upper.S05E03.mkv");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithoutPath_WhenFlattenEnabledAndSingleFile_ReturnsFileNameOnly()
    {
        // Arrange
        using var _ = WithMoveSingleFilesToRoot(true);

        var download = new Download
        {
            Link = "https://fake.url/Fixer.Upper.S05E03.mkv",
            FileName = "Fixer.Upper.S05E03.mkv"
        };

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = "Fixer.Upper.S05E03.mkv"
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Fixer.Upper.S05E03.1080p.WEB.x264",
            RdFiles = JsonSerializer.Serialize(files)
        };

        // Act
        var path = DownloadHelper.GetDownloadPath(torrent, download);

        // Assert
        Assert.Equal("Fixer.Upper.S05E03.mkv", path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFlattenEnabledButMultipleFiles_KeepsTorrentSubdirectory()
    {
        // Arrange
        using var _ = WithMoveSingleFilesToRoot(true);

        var download = new Download
        {
            Link = "https://fake.url/file1.mkv",
            FileName = "file1.mkv"
        };

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = "file1.mkv"
            },
            new()
            {
                Path = "file2.mkv"
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Torrent Name",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads", torrent, download, fileSystem);

        // Assert — multi-file torrents are unaffected by the flatten setting
        var expectedPath = Path.Combine("/data/downloads", "Torrent Name", "file1.mkv");
        Assert.Equal(expectedPath, path);
    }

    [Fact]
    public void GetDownloadPath_WithPath_WhenFlattenDisabledAndSingleFile_KeepsTorrentSubdirectory()
    {
        // Arrange — default setting (disabled) preserves the legacy behavior
        using var _ = WithMoveSingleFilesToRoot(false);

        var download = new Download
        {
            Link = "https://fake.url/Fixer.Upper.S05E03.mkv",
            FileName = "Fixer.Upper.S05E03.mkv"
        };

        IList<DebridClientFile> files =
        [
            new()
            {
                Path = "Fixer.Upper.S05E03.mkv"
            }
        ];

        var torrent = new Torrent
        {
            RdName = "Fixer.Upper.S05E03.1080p.WEB.x264",
            RdFiles = JsonSerializer.Serialize(files)
        };

        var fileSystem = new MockFileSystem();

        // Act
        var path = DownloadHelper.GetDownloadPath("/data/downloads/tv", torrent, download, fileSystem);

        // Assert
        var expectedPath = Path.Combine("/data/downloads/tv", torrent.RdName, "Fixer.Upper.S05E03.mkv");
        Assert.Equal(expectedPath, path);
    }
}
