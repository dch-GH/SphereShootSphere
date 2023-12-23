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
	public bool IsHost => GameNetworkSystem.IsHost;

	public void OnConnectHost(Connection channel, GameObject pawn)
	{
		if ( !GameNetworkSystem.IsHost )
			return;

		Connection = Connection;
		Pawn = pawn;
	}

	[Broadcast]
	public void OnConnectClient( Guid channelId, string userName)
	{
		//Log.Info( $"OnConnectClient - {channelId} - {userName}" );
		ConnectionId = channelId;
		UserName = userName;
		Local = this;
	}

	public void OnDisconnect()
	{

	}

	public void Read( ByteStream net )
	{
		ConnectionId = net.Read<Guid>();
		UserName = net.Read<string>();
		Score = net.Read<int>();
	}

	public void Write( ref ByteStream net )
	{
		net.Write( ConnectionId );
		net.Write( UserName );
		net.Write( Score );
	}
}
