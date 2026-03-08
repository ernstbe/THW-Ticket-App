using FluentAssertions;
using THWTicketApp.Models;
using Xunit;

namespace THWTicketApp.Tests.Models;

public class AttachmentTests
{
    [Theory]
    [InlineData("image/png", true)]
    [InlineData("image/jpeg", true)]
    [InlineData("image/gif", true)]
    [InlineData("image/svg+xml", true)]
    [InlineData("application/pdf", false)]
    [InlineData("text/plain", false)]
    [InlineData(null, false)]
    public void IsImage_DetectsImageTypes(string? mimeType, bool expected)
    {
        var attachment = new Attachment { MimeType = mimeType };
        attachment.IsImage.Should().Be(expected);
    }

    [Theory]
    [InlineData(500, "500 B")]
    [InlineData(1024, "1.0 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1.0 MB")]
    [InlineData(2621440, "2.5 MB")]
    public void SizeFormatted_FormatsCorrectly(long size, string expected)
    {
        var attachment = new Attachment { Size = size };
        attachment.SizeFormatted.Should().Be(expected);
    }

    [Fact]
    public void SizeFormatted_ZeroBytes_ShowsBytes()
    {
        var attachment = new Attachment { Size = 0 };
        attachment.SizeFormatted.Should().Be("0 B");
    }
}
