using System.Threading.Channels;

namespace ATMP;

public class ClientComponent : Component, INetworkSerializable
{
	[Property] public Connection Connection { get; set; }
	[Property] public GameObject Pawn { get; set; }
	[Property] private Guid ConnId => ConnectionId;

	public Guid ConnectionId { get; private set; }

	public void OnConnect( Connection channel )
	{
		Connection = channel;
		ConnectionId = channel.Id;
	}

	public void OnDisconnect()
	{

	}

	public void Read( ByteStream stream )
	{
		ConnectionId = stream.Read<Guid>();
	}

	public void Write( ref ByteStream stream )
	{
		stream.Write( Connection.Id );
	}
}
