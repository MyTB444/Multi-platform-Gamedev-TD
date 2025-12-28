
    public interface IDamageable
    {
        void TakeDamage(float incomingDamage, bool spellDamageEnabled = false, bool isAntiInvisible = false, bool isAntiReinforced = false);
    }
