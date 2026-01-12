using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "MMO/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Info")]
    public int itemID;                      // ID único del item
    public string itemName;                 // Nombre del item
    [TextArea(2, 4)]
    public string description;              // Descripción del item
    public Sprite icon;                     // Icono para la UI

    [Header("Item Properties")]
    public ItemType itemType;               // Tipo de item (consumible, arma, etc.)
    public bool isStackable = false;        // ¿Se puede apilar?
    public int maxStackSize = 1;            // Máximo por stack (si es apilable)

    [Header("Item Stats")]
    public int goldValue = 10;              // Valor en oro
    public int healthRestore = 0;           // HP que restaura (si es consumible)
    public int manaRestore = 0;             // Mana que restaura (si es consumible)
    public int damageBonus = 0;             // Daño extra (si es arma)
    public int armorBonus = 0;              // Defensa extra (si es armadura)
}

public enum ItemType
{
    Consumable,     // Pociones, comida
    Weapon,         // Espadas, hachas, arcos
    Armor,          // Armaduras, escudos
    Quest,          // Items de quest
    Material,       // Materiales de crafteo
    Misc            // Otros
}
