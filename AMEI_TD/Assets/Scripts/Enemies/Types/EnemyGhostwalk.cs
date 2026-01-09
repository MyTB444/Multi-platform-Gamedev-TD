/// <summary>
/// Enemy immune to slow effects - always moves at base speed.
/// </summary>
public class EnemyGhostwalk : EnemyBase
{
    private float ghostBaseSpeed;

    protected override void Start()
    {
        base.Start();
        ghostBaseSpeed = GetBaseSpeed();
    }

    protected override void Update()
    {
        ForceBaseSpeed();
        base.Update();
    }

    private void ForceBaseSpeed()
    {
        var field = typeof(EnemyBase).GetField("enemySpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(this, ghostBaseSpeed);
        }
    }

    private float GetBaseSpeed()
    {
        var field = typeof(EnemyBase).GetField("baseSpeed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            return (float)field.GetValue(this);
        }
        return 0f;
    }

    public new void ApplySlow(float slowPercent, float duration, bool showVFX = true)
    {
        // Ghostwalk enemies ignore all slow effects
    }
}