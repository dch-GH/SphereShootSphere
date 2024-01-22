namespace ATMP;

public sealed class AbilityPickup : Component, Component.ITriggerListener
{
	[Property] public PlayerController.PlayerAbility Ability { get; set; }

	public void OnTriggerEnter( Collider other )
	{
		Log.Info( other );
		if ( other.GameObject.Components.TryGet<PlayerController>( out var player ) )
		{
			Log.Info( $"You picked up the {Ability} ability!" );
			player.Abilities |= Ability;
			GameObject.Destroy();
		}
	}

	public void OnTriggerExit( Collider other )
	{
	}
}
