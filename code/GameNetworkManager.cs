﻿using Sandbox.Network;
using System.Threading.Tasks;

namespace ATMP;

[Title( "GameNetworkManager" )]
[Category( "Networking" )]
[Icon( "electrical_services" )]
public sealed class GameNetworkManager : Component, Component.INetworkListener, INetworkSerializable
{
	public static GameNetworkManager Instance;

	/// <summary>
	/// Create a server (if we're not joining one)
	/// </summary>
	[Property] public bool StartServer { get; set; } = true;

	/// <summary>
	/// The prefab to spawn for the player to control.
	/// </summary>
	[Property] public GameObject PlayerPrefab { get; set; }

	/// <summary>
	/// A list of points to choose from randomly to spawn the player in. If not set, we'll spawn at the
	/// location of the NetworkHelper object.
	/// </summary>
	[Property] public List<GameObject> SpawnPoints { get; set; }
	public List<Vector3> SpawnPositions { get; private set; } = new();

	public Dictionary<Connection, ClientComponent> Clients { get; private set; }
	RealTimeSince sincePingUpdate;

	protected override void OnAwake()
	{
		base.OnAwake();
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

		// Find all the legacy hammer spawns and populate the list
		var spawns = Scene.GetAllComponents<MapObjectComponent>().Where( x => x.GameObject.Name.ToLowerInvariant().Contains( "info_player_start" ) ).ToArray();
		foreach ( var c in spawns )
		{
			SpawnPoints.Add( c.GameObject );
			SpawnPositions.Add( c.GameObject.Transform.Position );
		}

		if ( SpawnPoints.Count <= 0 )
		{
			var spawn = Scene.CreateObject( true );
			spawn.Transform.Position = Transform.Position + Vector3.Up * 48f;
			SpawnPoints.Add( spawn );
			SpawnPositions.Add( spawn.Transform.Position );
		}

		var startLocation = Vector3.Zero;

		if ( SpawnPoints is not null && SpawnPoints.Count > 0 )
		{
			if ( Random.Shared is null )
				Log.Error( $"Sbox fucked up, Random.Shared is null??" );

			startLocation = Random.Shared.FromList( SpawnPoints, default ).Transform.Position;
		}

		//Log.Info( $"Joiner: {channel.Id}, {channel.DisplayName}, {channel.Name}, {channel.Address}" );
		SpawnPlayerAsync( channel, startLocation );
	}

	private async void SpawnPlayerAsync( Connection channel, Vector3 startLocation )
	{
		PreSpawnClient( channel.Id );

		await GameTask.DelayRealtimeSeconds( 1.5f );

		// Spawn this object and make the client the owner
		var player = SceneUtility.Instantiate( PlayerPrefab, startLocation );
		player.Name = $"Player - {channel.DisplayName}";
		player.BreakFromPrefab();
		player.Network.Spawn( channel );

		var client = player.Components.GetOrCreate<ClientComponent>();
		Clients.Add( channel, client );
		client.OnConnectHost( channel, player );
		client.OnConnectClient( channel.Id, channel.DisplayName, channel.SteamId );
	}

	[Broadcast]
	private void PreSpawnClient( Guid channelId )
	{
		if ( channelId.IsReceiver() )
		{
			var cam = Scene.GetAllComponents<CameraComponent>().First();
			if ( cam is not null )
			{
				cam.Transform.Position = new Vector3( 1445.57f, -244.278f, 699.061f );
				cam.Transform.Rotation = Rotation.From( new Angles( 36.325f, 170.742f, 0f ) );
			}
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

	protected override void OnFixedUpdate()
	{
		if ( !GameNetworkSystem.IsHost )
			return;

		if ( sincePingUpdate < 1 )
			return;

		foreach ( var (conn, client) in Clients )
		{
			client.UpdatePingClient( conn.Id, conn.Ping );
		}

		sincePingUpdate = 0;
	}

	public void Write( ref ByteStream net )
	{
		net.Write( SpawnPositions.Count );
		foreach ( var x in SpawnPositions )
		{
			net.Write( x );
		}
	}

	public void Read( ByteStream net )
	{
		SpawnPositions ??= new();
		var count = net.Read<int>();
		for ( int i = 0; i < count; i++ )
		{
			SpawnPositions.Add( net.Read<Vector3>() );
		}
	}
}
