namespace ATMP;

public sealed class WorldChatter : Component
{
	[Property] private GameObject _worldChatPrefab;

	[Broadcast]
	public void WorldChatSend( string msg )
	{
		var go = _worldChatPrefab.Clone( Transform.Position + Vector3.Up * 64 );
		go.BreakFromPrefab();

		var text = go.Components.GetOrCreate<TextRenderer>();
		text.Text = msg.Trim();
	}
}
