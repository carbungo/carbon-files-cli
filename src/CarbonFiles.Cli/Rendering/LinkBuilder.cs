using CarbonFiles.Cli.Infrastructure;

namespace CarbonFiles.Cli.Rendering;

public sealed class LinkBuilder(Profile profile)
{
    public bool HasFrontend => !string.IsNullOrEmpty(profile.FrontendUrl);

    public string BucketUrl(string bucketId)
        => $"{profile.FrontendUrl}/buckets/{bucketId}";

    public string FileUrl(string bucketId, string filePath)
        => $"{profile.FrontendUrl}/buckets/{bucketId}/files/{EncodeFilePath(filePath)}";

    public string UploadUrl(string bucketId, string token)
        => $"{profile.FrontendUrl}/buckets/{bucketId}/upload?token={Uri.EscapeDataString(token)}";

    private static string EncodeFilePath(string filePath)
        => string.Join("/", filePath.Split('/').Select(Uri.EscapeDataString));
}
