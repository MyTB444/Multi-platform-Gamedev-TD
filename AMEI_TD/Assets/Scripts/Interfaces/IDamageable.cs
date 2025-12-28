public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo, bool spellDamageEnabled = false);
    void TakeDamage(float incomingDamage, bool spellDamageEnabled = false);
}