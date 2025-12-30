public interface IDamageable
{
    void TakeDamage(DamageInfo damageInfo, float vfxDamage = 0, bool spellDamageEnabled = false);
    void TakeDamage(float incomingDamage,float vfxDamage = 0, bool spellDamageEnabled = false);
}