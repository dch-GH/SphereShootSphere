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
	[Property] public GameObject CameraStart { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<SpawnPoint> SpawnPoints { get; set; }

	public Dictionary<Connection, ClientComponent> Clients { get; private set; }

	[Sync] public List<Guid> ClientGuids { get; set; }

	protected override void OnAwake()
	{
		var cam = Scene.GetAllComponents<CameraComponent>().First();
		if ( cam is not null )
		{
			cam.Transform.Position = CameraStart.Transform.Position;
			cam.Transform.Rotation = CameraStart.Transform.Rotation;

			var dof = cam.Components.Get<DepthOfField>( includeDisabled: true );
			// ISSUE: dof.Enabled doesnt work lol
			dof.FrontBlur = true;
			dof.BackBlur = true;
		}

		Local.OnSpawned += ( playerGameObject ) =>
		{
			if ( cam.Components.TryGet<DepthOfField>( out var dof ) )
			{
				dof.FrontBlur = false;
				dof.BackBlur = false;
			}
		};

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

	protected override void OnDestroy()
	{
		Local.OnSpawned = null;
		Instance = null;
		Chat.Current = null;
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

		// Create a client independently of the gameobject they will control.
		// The client' connection owns this as well as their actual player GameObject.
		var clientGameObject = Scene.CreateObject();
		clientGameObject.Name = $"Client - {channel.DisplayName}";

		var client = clientGameObject.Components.Create<ClientComponent>();
		client.Connect( channel );
		clientGameObject.NetworkSpawn( channel );
		Clients.Add( channel, client );

		// Everything we just set on the client component in client.Connect
		// won't be synced unless we Network.Spawn AFTER we set all that stuff.
		PreSpawnClient( channel.Id );
		var startLocation = Random.Shared.FromList( SpawnPoints, default ).Transform.Position;
		SpawnPlayerAsync( channel, client, startLocation );
	}

	// Host only
	private async void SpawnPlayerAsync( Connection channel, ClientComponent client, Vector3 position )
	{
		await GameTask.DelayRealtimeSeconds( 1.5f );

		// Spawn this object and make the client the owner
		var player = PlayerPrefab.Clone( position );
		player.BreakFromPrefab();
		player.Name = $"Player - {channel.DisplayName}";
		client.Pawn = player;
		player.NetworkSpawn( channel );
	}

	[Broadcast]
	private void PreSpawnClient( Guid channelId )
	{
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
