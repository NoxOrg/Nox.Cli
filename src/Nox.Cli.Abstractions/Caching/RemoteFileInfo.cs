namespace Nox.Cli.Abstractions.Caching;

public class RemoteFileInfo : IEquatable<RemoteFileInfo>
{
    public string Name { get; set; } = string.Empty;
    public int Size { get; set; } = 0;
    public string ShaChecksum { get; set; } = string.Empty;

    public override bool Equals(object? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        if (other.GetType() != this.GetType()) return false;
        return Equals((RemoteFileInfo)other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Size, ShaChecksum);
    }

    public bool Equals(RemoteFileInfo? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Name == other.Name && Size == other.Size && ShaChecksum == other.ShaChecksum;
    }
}

