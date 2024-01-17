using Sandbox.Network;
using System.Threading.Tasks;

namespace ATMP;

[Title( "GameNetwork" )]
[Category( "Networking" )]
[Icon( "circle" )]
public sealed class GameNetwork : Component, Component.INetworkListener
{
	public static GameNetwork Instance;

	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	/// 
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<SpawnPoint> SpawnPoints { get; set; }

	public Dictionary<Connection, ClientComponent> Clients { get; private set; }

	protected override void OnAwake()
	{
		// Find all the legacy hammer spawns and populate the list
		SpawnPoints ??= new();
		if ( SpawnPoints.Count <= 0 )
		{
			var spawns = Scene.GetAllComponents<SpawnPoint>();
			SpawnPoints = spawns.ToList();

			if ( SpawnPoints.Count <= 0 )
			{
				var spawn = Scene.CreateObject( true );
				spawn.Transform.Position = Transform.Position + Vector3.Up * 48f;
				var sc = spawn.Components.GetOrCreate<SpawnPoint>();
				SpawnPoints[0] = sc;
			}
		}

		Instance = this;
	}

	protected override async Task OnLoad()
	{
		if ( Scene.IsEditor )
			return;

		if ( StartServer && !GameNetworkSystem.IsActive )
		{
			Clients = new();
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );
			GameNetworkSystem.CreateLobby();
		}
	}

	/// <summary>
	/// A client is fully connected to the server. This is called on the host.
	/// </summary>
	public void OnActive( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' has joined the game" );

		if ( PlayerPrefab is null )
			return;

		//Log.Info( $"Joiner: {channel.Id}, {channel.DisplayName}, {channel.Name}, {channel.Address}" );
		var startLocation = Random.Shared.FromList( SpawnPoints, default ).Transform.Position;
		SpawnPlayerAsync( channel, startLocation );
	}

	private async void SpawnPlayerAsync( Connection channel, Vector3 startLocation )
	{
		PreSpawnClient( channel.Id );

		await GameTask.DelayRealtimeSeconds( 1.5f );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( startLocation );
		player.Name = $"Player - {channel.DisplayName}";
		player.BreakFromPrefab();

		var client = player.Components.GetOrCreate<ClientComponent>();
		client.Pawn = player;
		client.Connect( channel );
		Clients.Add( channel, client );

		// Everything we just set on the client component in client.Connect
		// won't be synced unless we Network.Spawn AFTER we set all that stuff.
		player.Network.Spawn( channel );
	}

	[TargetedRPC, ClientRPC, Broadcast]
	private void PreSpawnClient( Guid channelId )
	{
		var cam = Scene.GetAllComponents<CameraComponent>().First();
		if ( cam is not null )
		{
			cam.Transform.Position = new Vector3( 1445.57f, -244.278f, 699.061f );
			cam.Transform.Rotation = Rotation.From( new Angles( 36.325f, 170.742f, 0f ) );
		}
	}

	public void OnDisconnected( Connection conn )
	{
		if ( Clients.TryGetValue( conn, out var client ) )
		{
			client.OnDisconnectClient( conn.Id, conn.DisplayName, conn.SteamId, conn.IsHost );
			client.Pawn.Destroy();
			Clients.Remove( conn );
		}
	}
}
