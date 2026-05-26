public sealed partial class Player : Component, Component.IDamageable, PlayerController.IEvents, Global.ISaveEvents, IKillSource
{
	private static Player LocalPlayer { get; set; }
	public static Player FindLocalPlayer() => LocalPlayer;
	public static T FindLocalWeapon<T>() where T : BaseCarryable => FindLocalPlayer()?.GetComponentInChildren<T>( true );
	public static T FindLocalToolMode<T>() where T : ToolMode => FindLocalPlayer()?.GetComponentInChildren<T>( true );
}
