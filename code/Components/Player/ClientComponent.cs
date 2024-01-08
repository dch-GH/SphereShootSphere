using Sandbox.Network;

namespace ATMP;

public class ClientComponent : Component, INetworkSerializable
{
	public static ClientComponent Local;

	[Property] public Connection Connection { get; set; }
	[Property] public GameObject Pawn { get; set; }

	public bool IsLocal => ConnectionId == Connection.Local.Id;
	public Guid ConnectionId { get; private set; }
	public string UserName { get; private set; }
	public int Score { get; set; }
	public ulong SteamId { get; private set; }
	public bool Host { get; private set; }
	public bool IsHost => GameNetworkSystem.IsHost;

	public void OnConnectHost( Connection channel, GameObject pawn )
	{
		if ( !GameNetworkSystem.IsHost )
			return;

		Connection = Connection;
		Pawn = pawn;
	}

	[Broadcast]
	public void OnConnectClient( Guid channelId, string userName, ulong steamId, bool isHost )
	{
		LocalPlayer.OnSpawned?.Invoke(GameObject);
		Chat.Current?.AddEntry( userName, $"has joined the game.", steamId, true);
		//Log.Info( $"OnConnectClient - {channelId} - {userName}" );
		ConnectionId = channelId;
		UserName = userName;
		SteamId = steamId;
		Host = isHost;
		Local = this;
	}

	[Broadcast]
	public void OnDisconnectClient( Guid channelId, string userName, ulong steamId, bool isHost )
	{
		Chat.Current?.AddEntry( userName, $"has left the game.", steamId, true);
	}

	public void Read( ByteStream net )
	{
		ConnectionId = net.Read<Guid>();
		UserName = net.Read<string>();
		Score = net.Read<int>();
		SteamId = net.Read<ulong>();
		Host = net.Read<bool>();
	}

	public void Write( ref ByteStream net )
	{
		net.Write( ConnectionId );
		net.Write( UserName );
		net.Write( Score );
		net.Write( SteamId );
		net.Write( Host );
	}
}
