using Sandbox.Network;

namespace ATMP;

public class ClientComponent : Component
{
	[Property] public Connection Connection { get; set; }
	[Property] public GameObject Pawn { get; set; }

	public bool IsLocal => ConnectionId == Connection.Local.Id;
	[Sync] public Guid ConnectionId { get; set; }
	[Sync] public bool Host { get; set; }
	[Sync] public string UserName { get; set; }
	[Sync] public int Score { get; set; }
	[Sync] public ulong SteamId { get; set; }
	[Sync] public short Ping { get; set; }

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
		if ( !GameNetworkSystem.IsHost )
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
		GameObject.Destroy();
	}
}
