using System;

namespace ProjectAtlas.Application
{
    public sealed class VisitorPose
    {
        public int id;
        public string name, zone;
        public float x, y, z, yaw;
        public VisitorPose Copy() => (VisitorPose)MemberwiseClone();
    }
    public interface IVisitorSession : IDisposable
    {
        string Status { get; }
        bool Connected { get; }
        bool IsHost { get; }
        int LocalId { get; }
        string RoomCode { get; }
        VisitorPose[] Visitors { get; }
        void ConfigureRelay(string hostname, int port);
        void CreateRoom(string name, string catalogVersion);
        void JoinRoom(string code, string name, string catalogVersion);
        void Publish(VisitorPose pose);
        void Leave();
    }
}
