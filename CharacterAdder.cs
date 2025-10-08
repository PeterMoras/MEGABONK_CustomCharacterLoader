using Assets.Scripts.Audio.Music;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression;
using BepInEx;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Localization;
using Object = UnityEngine.Object;
using StatModifier = Assets.Scripts.Inventory__Items__Pickups.Stats.StatModifier;

namespace CustomCharacterLoader;

public class CharacterAdder
{
    private readonly DataManager _dataManager;
    private readonly JObject _assetJSON;
    private readonly AssetBundle _assetBundle;
    private uint _eCharacter;
    private ManualLogSource Log;
    private string _author;
    public CharacterAdder(DataManager dataManager, JObject assetJson, AssetBundle assetBundle, ManualLogSource Log)
    {
        _dataManager = dataManager;
        _assetJSON = assetJson;
        _assetBundle = assetBundle;
        this.Log = Log;
    }
    
    public ECharacter AddCustomCharacter()
    {
        //Load character must be run first because it gets the necessary meta info
        var character = LoadCharacter();
        //Log.LogInfo("Loaded character data");
        var skins = LoadSkins();
        //Log.LogInfo("Loaded skin data");
        var passive = LoadPassive();
        //Log.LogInfo("Loaded passive data");
        var weapon = LoadWeapon();
        //Log.LogInfo("Loaded weapon data");
        character.passive = passive;
        character.weapon = weapon;
        

        ECharacter eCharacter = (ECharacter) _eCharacter;
        EWeapon eWeapon = (EWeapon) _eCharacter;

        
        //Perform the necessary logic to correctly put the loaded custom character into the game
        //_dataManager.characterData.Add(eCharacter, character); //this is added in LoadCharacter due to garbage collection concerns
        //Log.LogInfo($"Added character {_eCharacter} to {_dataManager.characterData.Count}");


        
        _dataManager.weapons.Add(eWeapon, weapon);
        EffectManager.weaponNamesCache.Add(eWeapon,weapon.name);

        //add character to character list
        _dataManager.unsortedCharacterData.Add(character);
        //add weapon to weapon list
        _dataManager.unsortedWeapons.Add(weapon);
        
        Log.LogInfo("Loaded Custom Character: " + character.name);
        
        return (ECharacter) _eCharacter;
    }

    private Il2CppSystem.Collections.Generic.List<SkinData> LoadSkins()
    {
        JSkin[] jSkins = JSkin.FromJSON(_assetJSON["skins"].Cast<JArray>()) ;
        Il2CppSystem.Collections.Generic.List<SkinData> skins = new Il2CppSystem.Collections.Generic.List<SkinData>();
        _dataManager.skinData.Add((ECharacter)_eCharacter, skins);
        //Log.LogInfo("Skin count: "+jSkins.Length);
        int count = 0;
        foreach (var jskin in jSkins)
        {
            count++;
            //Log.LogInfo($"Load skin {count}");
            var skin = ScriptableObject.CreateInstance<SkinData>();
            skins.Add(skin);
            skin.author = _author;
            skin.character = (ECharacter) _eCharacter;
            skin.name = jskin.skinName;
            skin.localizedName = CreateUniqueLocalizedString("skinName" + count, jskin.skinName);
            skin.localizedDescription = CreateUniqueLocalizedString("skinDescription" + count, jskin.description);
            skin.serializedLocalizationKeysName = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
            skin.serializedLocalizationKeys = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
            //skin.skinType = jskin.skinType;
            skin.icon = LoadAsset<Texture2D>(jskin.iconPath);
            var matList = new Il2CppSystem.Collections.Generic.List<Material>();
            foreach(var matPath in jskin.materialPaths)
            {
                var mat = LoadAsset<Material>(matPath);
                matList.Add(mat);
            }

            skin.materials = matList.ToArray().Cast<Il2CppReferenceArray<Material>>();
        }

        return skins;
    }

    private WeaponData LoadWeapon()
    {
        var jWeapon = JWeapon.FromJSON(_assetJSON["weapon"].Cast<JObject>());//.ToObject<JWeapon>();
        //Log.LogInfo("Loaded Weapon JSON");
        WeaponData weapon = ScriptableObject.CreateInstance<WeaponData>();

        weapon.author = _author;
        weapon.eWeapon = (EWeapon) _eCharacter;
        weapon.name = jWeapon.weaponName;
        weapon.damageSourceName = jWeapon.weaponName;
        weapon.description = jWeapon.weaponDescription;
        weapon.localizedName = CreateUniqueLocalizedString("weaponName", jWeapon.weaponName);
        weapon.localizedDescription = CreateUniqueLocalizedString("weaponDescription", jWeapon.weaponDescription);
        weapon.serializedLocalizationKeysName = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
        weapon.serializedLocalizationKeys = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
        weapon.icon = LoadAsset<Texture2D>(jWeapon.iconPath);
        weapon.attack = LoadAsset<GameObject>(jWeapon.weaponAttackPath);
        weapon.baseStats = new Il2CppSystem.Collections.Generic.Dictionary<EStat, float>();
        weapon.AchievementRequirement = null;
        foreach (var stat in jWeapon.stats)
        {
            weapon.baseStats.Add((EStat)stat.Stat,stat.Value);
        }

        weapon.upgradeData = GameObject.Instantiate(ScriptableObject.CreateInstance<UpgradeData>());
        

        weapon.upgradeData.upgradeModifiers = jWeapon.UpgradeOptions;
        return weapon;
    }

    private CharacterData LoadCharacter()
    {
        var jCharacter = JCharacter.FromJSON(_assetJSON["character"].Cast<JObject>());
        CharacterData character = ScriptableObject.CreateInstance<CharacterData>();
        character.eCharacter = (ECharacter) jCharacter.eCharacter;
        //Have to add scriptable object to dataManager immediately, or it gets destroyed (possibly by garbage collection?)
        _dataManager.characterData.Add(character.eCharacter,character);

        _eCharacter = jCharacter.eCharacter;
        _author = jCharacter.author;
        
        character.author = jCharacter.author;
        character.eCharacter = (ECharacter) jCharacter.eCharacter;
        character.name = jCharacter.characterName;
        //Log.LogInfo(character.name);
        character.localizedName = CreateUniqueLocalizedString("characterName",jCharacter.characterName);
        //Log.LogInfo(character.localizedName.GetLocalizedString());
        character.serializedLocalizationKeysName = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
        character.serializedLocalizationKeys = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };

        character.description = jCharacter.characterDescription;
        //Log.LogInfo(character.description);

        character.localizedDescription = CreateUniqueLocalizedString("characterDescription",jCharacter.characterDescription);
        character.colliderHeight = jCharacter.colliderHeight;
        character.colliderWidth = jCharacter.colliderWidth;
        character.coolness = jCharacter.coolness;
        character.difficulty = jCharacter.difficulty;
        character.audioFootsteps = new AudioClip[0]; //jCharacter.audioFootsteps);
        character.themeSong = null; //jCharacter.themeSong;
        character.prefab = LoadAsset<GameObject>(jCharacter.prefabPath);
        character.icon = LoadAsset<Texture2D>(jCharacter.iconPath);
        character.statModifiers = jCharacter.statModifiers;
        character.StatCategoryRatios = jCharacter.categoryRatios;
        character.categoryRatios = new Il2CppSystem.Collections.Generic.Dictionary<EStatCategory, float>();
        foreach (var category in jCharacter.categoryRatios)
        {
            character.categoryRatios[category.category] = category.value;
        }
        
        // Log.LogInfo("Does character still exist?");
        // Log.LogInfo(character != null);
        // Log.LogInfo(character.name);


        return character;
    }

    public PassiveData LoadPassive()
    {
        var jPassive = JPassive.FromJSON(_assetJSON["passive"].Cast<JObject>());
        PassiveData passiveData = ScriptableObject.CreateInstance<PassiveData>();

        passiveData.dummyPassive = new PassiveAbility();
        passiveData.icon = LoadAsset<Texture2D>(jPassive.iconPath);
        passiveData.ePassive = jPassive.passive;
        passiveData.name = jPassive.passiveName;
        passiveData.Cast<Object>().name = jPassive.passiveName;
        passiveData.localizedName = CreateUniqueLocalizedString("passiveName", jPassive.passiveName);
        passiveData.localizedDescription = CreateUniqueLocalizedString("passiveDescription",jPassive.passiveDescription);
        
        
        return passiveData;
    }

    public void ReadJSON(string json)
    {
        var jobj = JObject.Parse(json);
    }

    public T LoadAsset<T>(string assetName) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        return _assetBundle.LoadAssetAsync(assetName, Il2CppType.Of<T>()).GetResult().Cast<T>();
    }

    public LocalizedString CreateUniqueLocalizedString(string key, string value)
    {
        string eCharStr = "" + _eCharacter;
        var uniqueKey = $"{_eCharacter}.{key}";
        return CustomCharacterLoaderPlugin.CreateLocalizedString(uniqueKey, value);
    }
}

[Serializable]
public struct JPassive
{
    public string passiveName;
    public string passiveDescription;
    public EPassive passive;
    public string iconPath;

    public static JPassive FromJSON(JObject jobj)
    {
        return new JPassive()
        {
            iconPath = jobj["iconPath"].ToObject<string>(),
            passiveName = jobj["passiveName"].ToObject<string>(),
            passive = (EPassive)jobj["passive"].ToObject<int>(),
            passiveDescription = jobj["passiveDescription"].ToObject<string>()
        };
    }
}

public class JTest
{
    public string author; // The author of the character.
}

[Serializable]
public struct JCharacter
{
    public string author; // The author of the character.
    public uint eCharacter; // Unique character identifier.
    public string assetBundleName; // Name of the asset bundle associated with the character.
    public string characterName; // Display name of the character.
    public string characterDescription; // Description of the character.
    public float colliderHeight; // The height of the character collider.
    public float colliderWidth; // The width of the character collider.
    public int coolness; // A coolness factor for the character.
    public int difficulty; // Difficulty level for the character.
    public Il2CppArrayBase<string> audioFootsteps; // Array of audio clips for character footsteps.
    public string themeSong; // Optional theme song for the character.
    public string prefabPath; // Path to the character's prefab file.
    public string iconPath; // Path to the character's icon file.
    public Il2CppSystem.Collections.Generic.List<StatModifier> statModifiers;
    public Il2CppSystem.Collections.Generic.List<StatCategoryRatio> categoryRatios;
    
    public static JCharacter FromJSON(JObject jobj)
    {
        return new JCharacter()
        {
            author = jobj["author"].ToObject<string>(),
            eCharacter = jobj["eCharacter"].ToObject<uint>(),
            assetBundleName = jobj["assetBundleName"].ToObject<string>(),
            characterName = jobj["characterName"].ToObject<string>(),
            characterDescription = jobj["characterDescription"].ToObject<string>(),
            colliderHeight = jobj["colliderHeight"].ToObject<float>(),
            colliderWidth = jobj["colliderWidth"].ToObject<float>(),
            coolness = jobj["coolness"].ToObject<int>(),
            difficulty = jobj["difficulty"].ToObject<int>(),
            audioFootsteps = jobj["audioFootsteps"].ToObject<Il2CppArrayBase<string>>(),
            themeSong = jobj["themeSong"].ToObject<string>(),
            prefabPath = jobj["prefabPath"].ToObject<string>(),
            iconPath = jobj["iconPath"].ToObject<string>(),
            statModifiers = jobj["statModifiers"].ToObject<Il2CppSystem.Collections.Generic.List<StatModifier>>(),
            categoryRatios = jobj["categoryRatios"].ToObject<Il2CppSystem.Collections.Generic.List<StatCategoryRatio>>(),
        };
    }
}

[Serializable]
public struct JWeapon
{
    public string weaponName; // Name of the weapon.
    public string weaponDescription; // Description of the weapon.
    public Il2CppSystem.Collections.Generic.List<StatModifier> UpgradeOptions; // List of upgrade options for the weapon.
    public List<BaseStat> stats; // List of stats and their values.
    public string iconPath; // Path to the weapon's icon.
    public string weaponAttackPath; // Path to the weapon's attack prefab.

    public static JWeapon FromJSON(JObject jobj)
    {
        var baseStats = new List<BaseStat>();
        var jStats = jobj["stats"].Cast<JArray>();
        foreach (var jStat in jStats._values)
        {
            var stat = new BaseStat()
            {
                Stat = jStat.Cast<JObject>()["Stat"].ToObject<int>(),
                Value = jStat.Cast<JObject>()["Value"].ToObject<float>()
            };
            
            baseStats.Add(stat);
        }
        
        return new JWeapon()
        {
            weaponName = jobj["weaponName"].ToObject<string>(),
            weaponDescription = jobj["weaponDescription"].ToObject<string>(),
            UpgradeOptions = jobj["UpgradeOptions"].ToObject<Il2CppSystem.Collections.Generic.List<StatModifier>>(),
            stats = baseStats,
            iconPath = jobj["iconPath"].ToObject<string>(),
            weaponAttackPath = jobj["weaponAttackPath"].ToObject<string>(),
        };
    }

    [Serializable]
    public struct BaseStat
    {
        public int Stat;
        public float Value;
    }
}

[Serializable]
public struct JSkin
{
    public string skinName; // Name of the skin.
    public string description; // Description of the skin.
    public string iconPath; // Path to the skin's icon.
    public Il2CppSystem.Collections.Generic.List<string> materialPaths; // List of paths to the materials used for the skin.


    public static JSkin[] FromJSON(JArray jobj)
    {

        var jSkins = new List<JSkin>();
        foreach (var jskinToken in jobj._values)
        {
            var jSkin = jskinToken.Cast<JObject>();
            jSkins.Add(new JSkin()
            {
                skinName = jSkin["skinName"].ToObject<string>(),
                description = jSkin["description"].ToObject<string>(),
                iconPath = jSkin["iconPath"].ToObject<string>(),
                materialPaths = jSkin["materialPaths"].ToObject<Il2CppSystem.Collections.Generic.List<string>>()
            });
        }

        return jSkins.ToArray();
    }
}