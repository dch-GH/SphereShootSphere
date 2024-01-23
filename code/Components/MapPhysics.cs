namespace ATMP;

public sealed class MapPhysics : Component
{
	public void OnMapLoaded()
	{
		foreach ( var child in GameObject.Children )
		{
			if ( child.Components.TryGet<Collider>( out var _ ) )
			{
				child.Tags.Add( GameTags.World );
			}
		}
	}
}
