using UnityEngine;

public enum ElementType
{
    Physical,
    Magic,
    Mechanic,
    Imaginary
}

[System.Serializable]
public struct DamageInfo
{
    public float amount;
    public ElementType elementType;

    public DamageInfo(float amount, ElementType elementType)
    {
        this.amount = amount;
        this.elementType = elementType;
    }
}