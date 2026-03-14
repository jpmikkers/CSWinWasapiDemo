namespace Baksteen.Waves;

public record class AudioDeviceInfo
{
    public AudioDeviceId Id { get; set; } = new AudioDeviceId();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public decimal Manufacturer { get; internal set; } = 0M;
    public string? InterfaceFriendlyName { get; internal set; }
}
