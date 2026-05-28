/// <summary>
/// Holds persistent player information like deaths, kills
/// </summary>
public sealed partial class PlayerData : Component
{
	[Sync( SyncFlags.FromHost )] public int Kills { get; internal set; }
	[Sync( SyncFlags.FromHost )] public int Deaths { get; internal set; }
	[Sync( SyncFlags.FromHost )] public bool IsGodMode { get; internal set; }

	/// <summary>
	/// Is this player data me?
	/// </summary>
	public bool IsMe => Network.Owner == Connection.Local;

	/// <summary>
	/// Data for all players
	/// </summary>
	public static IEnumerable<PlayerData> All => Game.ActiveScene.GetAll<PlayerData>();

	/// <summary>
	/// Get player data for a player
	/// </summary>
	public static PlayerData For( Connection connection ) => connection == null ? default : All.FirstOrDefault( x => x.Network.Owner == connection );

	// Host-side respawn tracking. No sync required.
	private bool _needsRespawn;
	private RealTimeSince _timeSinceDied;

	/// <summary>
	/// Called on the host when the player dies. Starts the respawn countdown so that
	/// PlayerData can trigger a respawn if the PlayerObserver is destroyed (e.g. by cleanup)
	/// before it fires.
	/// </summary>
	internal void MarkForRespawn()
	{
		_needsRespawn = true;
		_timeSinceDied = 0;
	}

	/// <summary>
	/// Called by PlayerObserver (owner-only RPC) when the player presses to respawn early,
	/// or by OnUpdate after the timeout. Single entry point for all respawn logic.
	/// </summary>
	[Rpc.Host( NetFlags.OwnerOnly | NetFlags.Reliable )]
	internal void RequestRespawn()
	{
		_needsRespawn = false;

		// Clean up any lingering observer for this connection.
		foreach ( var observer in Scene.GetAllComponents<PlayerObserver>().Where( x => x.Network.Owner == Network.Owner ).ToArray() )
		{
			observer.GameObject.Destroy();
		}

		GameManager.Current?.SpawnPlayer( this );
	}

	protected override void OnUpdate()
	{
		if ( !Networking.IsHost ) return;
		if ( !_needsRespawn ) return;
		if ( _timeSinceDied < 4f ) return;

		RequestRespawn();
	}

	[Rpc.Broadcast]
	private void RpcAddStat( string identifier, int amount = 1 )
	{
		Sandbox.Services.Stats.Increment( identifier, amount );
	}

	/// <summary>
	/// Called on the host, calls a RPC on the player and adds a stat
	/// </summary>
	/// <param name="identifier"></param>
	/// <param name="amount"></param>
	internal void AddStat( string identifier, int amount = 1 )
	{
		if ( Application.CheatsEnabled ) return;

		Assert.True( Networking.IsHost, "PlayerData.AddStat is host-only!" );

		using ( Rpc.FilterInclude( Network.Owner ) )
		{
			RpcAddStat( identifier, amount );
		}
	}

}
