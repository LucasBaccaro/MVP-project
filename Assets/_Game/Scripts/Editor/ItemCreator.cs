using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Editor Window para crear items fácilmente
/// </summary>
public class ItemCreator : EditorWindow
{
    [MenuItem("MMO/Item Creator")]
    public static void ShowWindow()
    {
        GetWindow<ItemCreator>("Item Creator");
    }

    [MenuItem("MMO/Create Default Items")]
    public static void CreateDefaultItems()
    {
        string folderPath = "Assets/_Game/ScriptableObjects/Items";

        // Crear carpeta si no existe
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            string parentFolder = "Assets/_Game/ScriptableObjects";
            if (!AssetDatabase.IsValidFolder(parentFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Game", "ScriptableObjects");
            }
            AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "Items");
        }

        // 1. Poción de Salud
        ItemData healthPotion = ScriptableObject.CreateInstance<ItemData>();
        healthPotion.itemID = 1;
        healthPotion.itemName = "Poción de Salud";
        healthPotion.description = "Restaura 50 puntos de vida.";
        healthPotion.itemType = ItemType.Consumable;
        healthPotion.isStackable = true;
        healthPotion.maxStackSize = 20;
        healthPotion.goldValue = 25;
        healthPotion.healthRestore = 50;
        healthPotion.manaRestore = 0;
        healthPotion.damageBonus = 0;
        healthPotion.armorBonus = 0;
        AssetDatabase.CreateAsset(healthPotion, $"{folderPath}/HealthPotion.asset");

        // 2. Poción de Maná
        ItemData manaPotion = ScriptableObject.CreateInstance<ItemData>();
        manaPotion.itemID = 2;
        manaPotion.itemName = "Poción de Maná";
        manaPotion.description = "Restaura 30 puntos de maná.";
        manaPotion.itemType = ItemType.Consumable;
        manaPotion.isStackable = true;
        manaPotion.maxStackSize = 20;
        manaPotion.goldValue = 20;
        manaPotion.healthRestore = 0;
        manaPotion.manaRestore = 30;
        manaPotion.damageBonus = 0;
        manaPotion.armorBonus = 0;
        AssetDatabase.CreateAsset(manaPotion, $"{folderPath}/ManaPotion.asset");

        // 3. Espada de Hierro
        ItemData ironSword = ScriptableObject.CreateInstance<ItemData>();
        ironSword.itemID = 3;
        ironSword.itemName = "Espada de Hierro";
        ironSword.description = "Una espada simple pero efectiva. +10 de daño.";
        ironSword.itemType = ItemType.Weapon;
        ironSword.isStackable = false;
        ironSword.maxStackSize = 1;
        ironSword.goldValue = 100;
        ironSword.healthRestore = 0;
        ironSword.manaRestore = 0;
        ironSword.damageBonus = 10;
        ironSword.armorBonus = 0;
        AssetDatabase.CreateAsset(ironSword, $"{folderPath}/IronSword.asset");

        // 4. Escudo de Madera
        ItemData woodenShield = ScriptableObject.CreateInstance<ItemData>();
        woodenShield.itemID = 4;
        woodenShield.itemName = "Escudo de Madera";
        woodenShield.description = "Un escudo básico. +5 de armadura.";
        woodenShield.itemType = ItemType.Armor;
        woodenShield.isStackable = false;
        woodenShield.maxStackSize = 1;
        woodenShield.goldValue = 50;
        woodenShield.healthRestore = 0;
        woodenShield.manaRestore = 0;
        woodenShield.damageBonus = 0;
        woodenShield.armorBonus = 5;
        AssetDatabase.CreateAsset(woodenShield, $"{folderPath}/WoodenShield.asset");

        // 5. Moneda de Oro
        ItemData goldCoin = ScriptableObject.CreateInstance<ItemData>();
        goldCoin.itemID = 5;
        goldCoin.itemName = "Moneda de Oro";
        goldCoin.description = "Una moneda de oro brillante.";
        goldCoin.itemType = ItemType.Misc;
        goldCoin.isStackable = true;
        goldCoin.maxStackSize = 999;
        goldCoin.goldValue = 1;
        goldCoin.healthRestore = 0;
        goldCoin.manaRestore = 0;
        goldCoin.damageBonus = 0;
        goldCoin.armorBonus = 0;
        AssetDatabase.CreateAsset(goldCoin, $"{folderPath}/GoldCoin.asset");

        // Guardar cambios
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ 5 items creados en: {folderPath}");
        EditorUtility.DisplayDialog("Items Creados",
            "Se crearon 5 items de ejemplo:\n\n" +
            "1. Poción de Salud\n" +
            "2. Poción de Maná\n" +
            "3. Espada de Hierro\n" +
            "4. Escudo de Madera\n" +
            "5. Moneda de Oro\n\n" +
            "Ahora puedes asignarlos al ItemDatabase en la escena GameWorld.",
            "OK");
    }

    void OnGUI()
    {
        GUILayout.Label("Item Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Create Default Items", GUILayout.Height(40)))
        {
            CreateDefaultItems();
        }

        GUILayout.Space(10);
        GUILayout.Label("Esto creará 5 items de ejemplo:\n" +
            "- Poción de Salud\n" +
            "- Poción de Maná\n" +
            "- Espada de Hierro\n" +
            "- Escudo de Madera\n" +
            "- Moneda de Oro", EditorStyles.helpBox);
    }
}
