namespace Nox.Cli.Configuration;

public class RemoteFileInfo : IEquatable<RemoteFileInfo>
{
    public string Name { get; set; } = string.Empty;
    public int Size { get; set; } = 0;
    public string ShaChecksum { get; set; } = string.Empty;

    public bool Equals(RemoteFileInfo? other) => ShaChecksum == other?.ShaChecksum;
    public override int GetHashCode() => ShaChecksum.GetHashCode();
}

