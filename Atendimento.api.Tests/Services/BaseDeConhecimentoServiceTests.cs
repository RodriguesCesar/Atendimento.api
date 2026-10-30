using Atendimento.API.Services;
using Xunit;

namespace Atendimento.API.Tests.Services;

public class BaseDeConhecimentoServiceTests
{
    [Fact]
    public void LoadDocuments_WhenDirectoryDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        var sut = new BaseDeConhecimentoService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));

        Assert.Throws<DirectoryNotFoundException>(() => sut.LoadDocuments());
    }

    [Fact]
    public void LoadDocuments_WhenMarkdownExists_LoadsAndChunksContent()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);

        try
        {
            var markdown = "# Título\n\n" + new string('A', 700);
            var filePath = Path.Combine(tempPath, "faq.md");
            File.WriteAllText(filePath, markdown);

            var sut = new BaseDeConhecimentoService(tempPath);

            var docs = sut.LoadDocuments();

            Assert.Single(docs);
            Assert.Equal("faq.md", docs[0].FileName);
            Assert.NotEmpty(docs[0].Chunks);
            Assert.True(docs[0].Chunks.Count >= 2);
        }
        finally
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }
}
