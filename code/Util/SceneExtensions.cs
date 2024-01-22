using ATMP;

namespace Sandbox;

public static class SceneExtensions
{
	// public static GameObject InstantiatePath(this Scene _, string prefabPath, Vector3 position, Rotation rotation )
	// 	=> SceneUtility.Instantiate( SceneUtility.GetPrefabScene( ResourceLibrary.Get<PrefabFile>( prefabPath ) ), position, rotation );

	public static ClientComponent FindClient( this Scene self, Guid id ) => self.GetAllComponents<ClientComponent>().First( x => x.ConnectionId == x.GameObject.Network.OwnerId );
	public static ClientComponent FindClient( this Scene self, GameObject obj ) => FindClient( self, obj.Network.OwnerId );
}
