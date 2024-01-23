using Sandbox.Network;

namespace ATMP;

public class ClientComponent : Component
{
    [Property] public Connection Connection { get; set; }
    [Property] public GameObject Pawn { get; set; }

    public bool IsLocal => ConnectionId == Connection.Local.Id;
    [Sync] public Guid ConnectionId { get; private set; }
    [Sync] public bool Host { get; private set; }
    [Sync] public string UserName { get; private set; }
    [Sync( Query = true )] public int Score { get; private set; }
    [Sync] public ulong SteamId { get; private set; }
    [Sync] public short Ping { get; private set; }

    private RealTimeSince _lastPingUpdate;

    public void Connect( Connection channel )
    {
        Connection = channel;
        Host = channel.IsHost;
        ConnectionId = channel.Id;
        UserName = channel.DisplayName;
        SteamId = channel.SteamId;
    }

    protected override void OnStart()
    {
        if ( IsProxy )
            return;

        Local.Client = this;
    }

    protected override void OnFixedUpdate()
    {
        if ( !Networking.IsHost )
            return;

        if ( _lastPingUpdate < 1 )
            return;

        Ping = (short)(Connection.Ping.FloorToInt());
        _lastPingUpdate = 0;
    }

    [Broadcast]
    public void OnDisconnectClient( Guid channelId, string userName, ulong steamId, bool isHost )
    {
        Chat.Current?.AddEntry( userName, $"has left the game.", steamId, true );

        if ( !IsProxy )
            GameObject.Destroy();
    }

    // [ServerRPC, Broadcast]
    public void AddScore( int amount )
    {
        Score += amount;
    }
}
