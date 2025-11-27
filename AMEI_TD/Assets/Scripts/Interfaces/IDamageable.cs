public interface IDamageable
{
    void TakeDamage(float incomingDamage, bool isAntiInvisible = false, bool isAntiReinforced = false);
}