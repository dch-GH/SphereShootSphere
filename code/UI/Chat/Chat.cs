namespace Sandbox;

public partial class Chat
{
	[Broadcast]
	public static void AddChatEntry( string name, string message, ulong steamId, bool isInfo = false )
	{
		// Again in the RPC.
		message = message.RemoveBadCharacters();

		if ( Rpc.Caller.SteamId != steamId )
			return;

		Current?.AddEntry( name, message, steamId, isInfo );
		Log.Info( $"{name}: {message}" );
	}

	public static void Say( string message )
	{
		// Clean it clientside.
		message = message.RemoveBadCharacters();

		var user = Connection.Local;
		AddChatEntry( user.DisplayName, message, user.SteamId );
	}
}
