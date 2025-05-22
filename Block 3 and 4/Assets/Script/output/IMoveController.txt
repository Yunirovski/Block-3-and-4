/// <summary>
/// Interface for movement control, allowing equipment to modify the player¡¯s walk and run speeds at runtime.
/// </summary>
public interface IMoveController
{
    /// <summary>
    /// Adjusts the character¡¯s base movement speed by the given multiplier.
    /// A value of 1.0 restores the original speed; values >1.0 increase speed, values between 0 and 1.0 slow the character down.
    /// </summary>
    /// <param name="multiplier">
    /// The factor by which to multiply the character¡¯s base speed.
    /// </param>
    void ModifySpeed(float multiplier);
}
