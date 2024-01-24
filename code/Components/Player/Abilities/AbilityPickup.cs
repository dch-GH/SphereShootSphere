using ATMP.UI;
using Sandbox.Network;
using static ATMP.PlayerController;

namespace ATMP;

public sealed class AbilityPickup : Component, Component.ITriggerListener
{
	[Property] public PlayerAbility Ability { get; set; }
	[Property] public string ToastTitle { get; set; }
	[Property] public string ToastSubtitle { get; set; }
	[Property] public Texture ToastIcon { get; set; }
	private RealTimeUntil _shouldDespawn = 30;

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost )
			return;

		if ( _shouldDespawn )
			GameObject.Destroy();
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( other.GameObject.Components.TryGet<PlayerController>( out var player ) )
		{
			OnPickedUp( player.GameObject.Id );
		}
	}

	public void OnTriggerExit( Collider other )
	{
	}

	[Broadcast]
	public void OnPickedUp( Guid playerGameObjectId )
	{
		if ( playerGameObjectId != Local.Pawn.Id )
			return;

		var player = Local.Player;
		if ( !player.IsValid() || player.HasAbility( Ability ) )
			return;

		if ( player == Local.Player )
		{
			Log.Info( $"You picked up the {Ability} ability!" );
			ToastDock.Instance.Toast( this );
			player.Abilities |= Ability;
			AfterPickup();
		}
	}

	[Authority]
	public void AfterPickup()
	{
		GameObject.Destroy();
	}
}
