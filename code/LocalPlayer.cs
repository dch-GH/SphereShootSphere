using ATMP;

namespace Sandbox;

public static class LocalPlayer
{
	public static GameObject Pawn;
	public static Action<GameObject> OnSpawned;
	public static ClientComponent Client;
	public static bool IsHost => Client.Host;
}
