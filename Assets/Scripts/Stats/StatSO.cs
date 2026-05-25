using UnityEngine;

[CreateAssetMenu(fileName = "StatSO", menuName = "Scriptable Objects/StatSO")]
public class StatSO : ScriptableObject
{
    public float MaxHP;
    public float MaxMP;
    public float MaxStamina;
    public float HPRegen;
    public float MPRegen;
    public float StaminaRegen; 
    public float ATK;
    public float MAG;
    public float DEF;
    public float MDEF;
    public float CritChance;
    public float CritDamage;
    public float MS;
    public float AS;
    public float DodgeChance;
    public float BlockChance;
    public float CooldownReduction;
}
