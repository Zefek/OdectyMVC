namespace OdectyMVC.Models
{
    public class FirmwareManifest
    {
        public int Version { get; set; }
        public string? File { get; set; }
        public long? Size { get; set; }
        public string? Sha256 { get; set; }
        public string? Commit { get; set; }
    }
}
