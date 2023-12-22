namespace Sandbox;

public class SceneParticleManager : Component
{
	[Property] public ParticleSystem Template { get; set; }
	private List<SceneParticles> _list;

	protected override void OnAwake()
	{
		base.OnAwake();
		_list ??= new( 5 );
	}

	protected override void OnEnabled()
	{
		_list ??= new( 5 );
		base.OnEnabled();
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		foreach ( var p in _list )
			p.Delete();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		foreach ( var p in _list )
			p.Delete();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		foreach ( var p in _list )
		{
			p.Simulate( Time.Delta );
			if ( p.Finished )
			{
				p.Delete();
			}
		}

		_list.RemoveAll( x => x.Finished );
	}

	public SceneParticles Spawn()
	{
		var p = new SceneParticles( Scene.SceneWorld, Template );
		p.RenderingEnabled = true;
		p.EmissionStopped = false;
		p.SimulationTime = 0;

		_list.Add( p );
		return p;
	}

}
