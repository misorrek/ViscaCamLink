namespace ViscaCamLink.Repositories.AppSettings
{
    public sealed class CameraProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Name { get; set; } = "Camera";

        public string Ip { get; set; } = "192.168.0.1";

        public int Port { get; set; } = 5678;
    }
}
