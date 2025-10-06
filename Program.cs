
using Assets.Scripts.Actors.Player;
using Assets.Scripts.Game.Combat;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive;
using Assets.Scripts.Inventory__Items__Pickups.AbilitiesPassive.Implementations;
using Assets.Scripts.Inventory__Items__Pickups.Stats;
using Assets.Scripts.Inventory__Items__Pickups.Upgrades;
using Assets.Scripts.Inventory__Items__Pickups.Weapons.Attacks;
using Assets.Scripts.Managers;
using Assets.Scripts.Menu.Shop;
using Assets.Scripts.Saves___Serialization.Progression.Achievements;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.UnityEngine;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.Collections;
using Il2CppSystem.IO;
using Il2CppSystem.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Input = UnityEngine.Input;
using KeyCode = UnityEngine.KeyCode;

namespace MyPlugins;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static readonly string ASSET_BUNDLE_NAME = "charactermaker";
    public GameObject BepInExUtility;
    public override void Load()
    {
        Debug.Log("Load MyPlugin");
        Log.LogInfo("Started Loading MyPlugins");

        var assetPath = Il2CppSystem.IO.Path.Combine(Paths.PluginPath, ASSET_BUNDLE_NAME);
        var s = Il2CppSystem.IO.File.OpenRead(assetPath);
        var mainAssetBundle = AssetBundle.LoadFromStream(s);
        Log.LogDebug(mainAssetBundle.name);
        
        ClassInjector.RegisterTypeInIl2Cpp<InjectComponent>();
        BepInExUtility = GameObject.Find("BepInExUtility");

        if (BepInExUtility == null)
        {
            BepInExUtility = new GameObject("BepInExUtility");
            GameObject.DontDestroyOnLoad(BepInExUtility);
            BepInExUtility.hideFlags = HideFlags.HideAndDontSave;
            BepInExUtility.AddComponent<InjectComponent>();
        }
        else BepInExUtility.AddComponent<InjectComponent>();
        var inject = BepInExUtility.GetComponent<InjectComponent>();
        inject.Log = this.Log;
        inject.assetBundle = mainAssetBundle;
        InjectComponent.Instance = inject;
        Log.LogDebug("Finished loading InjectComponent");

        Harmony harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        Log.LogDebug("Patched harmony methods");
        Log.LogInfo("Finished Loading MyPlugins");

    }

    [HarmonyPatch(typeof(DataManager))]
    [HarmonyPatch(nameof(DataManager.Load))]
    public static class DataManagerLoadCustomCharacterPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(DataManager __instance)
        {
            Debug.Log("Apply MyPlugins postfix");
            InjectComponent.Instance.InitCustomCharacter();
        }
    }


    private string m_SourceFolder = "dataFiles";
    private string m_DataFileName = "settings";

    public class InjectComponent : MonoBehaviour
    {
        public static InjectComponent Instance;
        
        public AssetBundle assetBundle;
        public ManualLogSource Log;
        // public MainMenu menu;
        // public CharacterMenu characterMenu;
        
        //needs to be added in IL2CPP to register properly I think
        public InjectComponent(IntPtr handle) : base(handle) { }

        private void Start()
        {
            //Activate();
        }

        internal void Update()
        {
            // if (Input.GetKeyDown(KeyCode.H))
            // {
            //     Log.LogInfo("Pressed H");
            //     Activate();
            // }
        }

        void TryModifyStringTable()
        {
            var stringTable = LocalizationSettings.StringDatabase.GetTable("Main Menu");
            // var stringDB = LocalizationSettings.Instance.GetStringDatabase();
            //
            // var tableReq = stringDB.GetTableAsync("Main Menu");
            // Addressables.ResourceManager.Acquire(tableReq);
            // stringTable = tableReq.Result;
            var entr =  stringTable.AddEntry("MYKEY", "MYVALUE");
            stringTable.AddEntry("MYKEY2".GetHashCode(), "MYVALUE");
            Log.LogInfo(entr.GetLocalizedString() + " : " + entr.Value);
            Log.LogInfo(entr.Table.TableCollectionName + " : " + entr.Key);
            Log.LogInfo(new LocalizedString(entr.Table.TableCollectionName,entr.Key).GetLocalizedString());
            Log.LogInfo(new LocalizedString(entr.Table.TableCollectionName,"MYKEY2".GetHashCode()).GetLocalizedString());

            foreach (var entry in stringTable.m_TableEntries._entries)
            {
                Log.LogInfo(entry.key + " : " + entry.value.Value);
                Log.LogInfo(new LocalizedString(stringTable.name,entry.key).GetLocalizedString());
            }
            
            // stringTable.AddEntryFromReference(new TableEntryReference() { Key = "NEWTEST", KeyId = 27 },
            //     "My NEWTEST value");
            
            // var entry = stringTable.GetEntry("PLAY");
            // Log.LogInfo("PLAY VALUE: " + entry.Value);
            // entry.Value = "AASGHGHGGH";
            // stringTable.AddEntry("TEST", "ADDED VALUE");
            // entry = stringTable.AddEntry(494562, "en");
            //
            // entry.Value = "TEST ADDED VALUE";
            // var tableEntry = new TableEntryData((long)546544, "TESTING_VALUE");
            // stringTable.TableData.Add(tableEntry);
            // var entry = stringTable.CreateTableEntry(tableEntry); 
            // Log.LogInfo(entry.Key + " : " + entry.Value + " : " + entry.KeyId);
            //
            var ls1 = new LocalizedString("Main Menu", 5464L);
            foreach (var data in stringTable.TableData._items)
            {
                if (data == null) continue;
                Log.LogInfo(data.Id + " : " + data.Localized);
                
                var ls = new LocalizedString("Main Menu", data.Id);
                Log.LogInfo(ls.ToString() + " | "+ls.GetLocalizedString());
                
            }
            
            // Log.LogInfo("Is Main Menu table loaded: " + LocalizationSettings.StringDatabase.IsTableLoaded("Main Menu"));
            // foreach (var data in stringDB.GetTable("Main Menu").TableData._items)
            // {
            //     if (data == null) continue;
            //     Log.LogInfo(data.Id + " : " + data.Localized);
            //     
            //     var ls = new LocalizedString("Main Menu", data.Id);
            //     Log.LogInfo(ls.GetLocalizedString());
            //     
            // }
            // Log.LogInfo(ls1.GetLocalizedString());
            // var ls2 = new LocalizedString("Main Menu", 5464L);
            // Log.LogInfo(ls2.GetLocalizedString());

          //  stringTable.GetEntry("Play")
        }

        public void InitCustomCharacter()
        {
            //TryModifyStringTable();
            Log.LogInfo(assetBundle.name);
            Log.LogInfo(assetBundle.mainAsset);
            var myWeapon = assetBundle.LoadAssetAsync("WeaponData", Il2CppType.Of<UnityEngine.Object>()).GetResult();
            var weaponData = myWeapon.Cast<WeaponData>();
            var myPassive = assetBundle.LoadAssetAsync("PassiveData", Il2CppType.Of<UnityEngine.Object>()).GetResult();
            var passiveData = myPassive.Cast<PassiveData>();
            var myCharacter = assetBundle.LoadAssetAsync("CharacterData", Il2CppType.Of<UnityEngine.Object>()).GetResult();
            var characterData = myCharacter.Cast<CharacterData>();
            Log.LogInfo("Loaded Custom Character data");
            var mySkin = assetBundle.LoadAssetAsync("SkinData", Il2CppType.Of<UnityEngine.Object>()).GetResult();
            var skinData = mySkin.Cast<SkinData>();
            Log.LogInfo("Loaded Custom skin data");

            
            var myProjectile = assetBundle.LoadAssetAsync("MyProjectile",Il2CppType.Of<UnityEngine.Object>() ).GetResult().Cast<GameObject>();
            Log.LogInfo(myProjectile.name);
            Log.LogInfo("Loaded custom projectile");
            
            // var customScriptDataRequest = assetBundle.LoadAssetAsync("MyCharacterData", Il2CppType.Of<UnityEngine.Object>());
            // Log.LogInfo("Found custom character script data" + customScriptDataRequest);
            // var customScriptData = customScriptDataRequest.GetResult().Cast<MyCharacterData>();
            // Log.LogInfo(customScriptData.name);
            Log.LogInfo("Loading Character Menu");
            var foxCharacterData = DataManager.Instance.unsortedCharacterData._items[0];
            Log.LogInfo("Loaded fox data");
            //var ogreCharacter = DataManager.Instance.characterData[ECharacter.Ogre];
            //Log.LogInfo(foxCharacterData.weapon.attack.name);

            bool useFoxBaseWeapon = true;
            if (useFoxBaseWeapon)
            {
                var tempWeapon = weaponData;
                tempWeapon.upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
                tempWeapon.upgradeData.upgradeModifiers = foxCharacterData.weapon.upgradeData.upgradeModifiers;
                var modifier = new StatModifier()
                {
                    modifyType = EStatModifyType.Multiplication,
                    modification = 10,
                    stat = EStat.DamageMultiplier
                };
                tempWeapon.upgradeData.upgradeModifiers.Add(modifier);
                tempWeapon.baseStats = new Il2CppSystem.Collections.Generic.Dictionary<EStat, float>();
                // foreach (var baseStat in foxCharacterData.weapon)
                // {
                //     tempWeapon.baseStats.TryAdd(baseStat.key, baseStat.value);
                //     Log.LogInfo(baseStat.key + " : " + baseStat.value);
                // }
                // tempWeapon.baseStats = foxCharacterData.weapon.baseStats;
                
                tempWeapon.baseStats.Add(EStat.AttackSpeed,5f);
                tempWeapon.baseStats.Add(EStat.Projectiles,10f);
                tempWeapon.baseStats.Add(EStat.SizeMultiplier,1f);
                tempWeapon.baseStats.Add(EStat.ProjectileBounces,0f);
                tempWeapon.baseStats.Add(EStat.DurationMultiplier,2f);
                tempWeapon.baseStats.Add(EStat.ProjectileSpeedMultiplier,0.5f);
                tempWeapon.baseStats.Add(EStat.KnockbackMultiplier,1f);
                tempWeapon.baseStats.Add(EStat.CritChance,0f);
                tempWeapon.baseStats.Add(EStat.DamageMultiplier,10f);
                tempWeapon.baseStats.Add(EStat.CritDamage,0f);

                tempWeapon.damageSourceName = tempWeapon.name;
                // tempWeapon.attackDuration = 1;
                // tempWeapon.spawnProjectileRange = 40;
                weaponData = tempWeapon;

                // weaponData = foxCharacterData.weapon;
                // weaponData.attack = tempWeapon.attack;
                // weaponData.icon = tempWeapon.icon;
                // weaponData.upgradeData = tempWeapon.upgradeData;
                // weaponData.baseStats = tempWeapon.baseStats;
                //
                // weaponData.description = tempWeapon.description;
                // weaponData.spawnOffset = Vector3.zero;
                // weaponData.AchievementRequirement = null;

            }
            else
            {
                //weaponData.upgradeData = foxCharacterData.weapon.upgradeData;
                weaponData.baseStats = foxCharacterData.weapon.baseStats;
                weaponData.onlySpawnWhenCloseEnemies = false;
            }
            
            
            

            //realCharacterData.weapon = weaponData;
            
            //var explodeScript = realCharacterData.weapon.attack.GetComponent<WeaponAttack>().prefabProjectile.GetComponent<ProjectileExploding>();
            //realCharacterData.weapon = otherCharacter.weapon;
            //Log.LogInfo(explodeScript.name);
            //var effect = myProjectile.GetComponent<ProjectileExploding>().explosionEffect;
            // CopyComponent(myProjectile, explodeScript);
            // realCharacterData.weapon.attack.GetComponent<WeaponAttack>().prefabProjectile.GetComponent<ProjectileExploding>().explosionEffect = myProjectile.GetComponent<ProjectileExploding>().explosionEffect;
            // myProjectile.GetComponent<ProjectileExploding>().explosionEffect = effect;
            
            
            //var skinData = DataManager.Instance.skinData[ECharacter.Fox];
            var skinList = new Il2CppSystem.Collections.Generic.List<SkinData>();
            skinList.Add(skinData);
            
            
            fillInCharacterData(characterData,skinList, weaponData, passiveData);
            //CustomCharacterMaker.AddNewCharacterToButtonMenu(characterMenu, characterData);
            
            //Add character to character list so it gets populated on menu startup
            DataManager.Instance.unsortedCharacterData.Add(characterData);
            
            //add weapon to weapon list
            DataManager.Instance.unsortedWeapons.Add(weaponData);
  
        }

        // internal void GetMenuUI()
        // {
        //     if(!menu)
        //         menu = FindFirstObjectByType<MainMenu>();
        //     if(menu && !characterMenu)
        //         characterMenu = CustomCharacterMaker.CharacterMenuFromUI(menu);
        // }

        public void fillInCharacterData(CharacterData characterData, Il2CppSystem.Collections.Generic.List<SkinData> skinData, WeaponData weaponData = null, PassiveData passiveData = null)
        {
            string localeCode = LocalizationSettings.SelectedLocale.Identifier.Code;
            Debug.Log($"Current Locale Code: {localeCode}");
            ECharacter eCharacterID = (ECharacter)2001;
            EWeapon eWeaponID = (EWeapon)2001;
            EPassive ePassiveID = (EPassive)2001;
            DataManager datamanager = DataManager.Instance;
            
            //UPPDATE CHARACTER DATA
            if (characterData != null)
            {
                characterData.eCharacter = eCharacterID;
                characterData.localizedName = CreateLocalizedString("coolgal_character_name", "Cool gal");
                characterData.localizedDescription = CreateLocalizedString("coolgal_character_description", "Cool gal is a cool gal");
                datamanager.characterData.Add(eCharacterID, characterData);

                foreach (var skin in skinData)
                {
                    skin.character = eCharacterID;
                }
                datamanager.skinData.Add(eCharacterID, skinData);
                SaveManager.Instance.config.preferences.characterSkins.TryAdd(eCharacterID,0);
            }
            
            
            //UPDATE WEAPON DATA
            if (weaponData != null)
            {
                weaponData.eWeapon = eWeaponID;
                var weaponName = "cool weapon";
                weaponData.localizedName = CreateLocalizedString("coolgal_weapon_name", weaponName);
                weaponData.localizedDescription = CreateLocalizedString("coolgal_weapon_description", "this is a cool weapon");
                datamanager.weapons.Add(eWeaponID, weaponData);
                characterData.weapon = weaponData;
                EffectManager.weaponNamesCache.Add(eWeaponID,weaponName);
            }

            if (passiveData != null)
            {
                passiveData.ePassive = ePassiveID;
                passiveData.localizedName = CreateLocalizedString("coolgal_passive_name", "cool passive");
                passiveData.localizedDescription = CreateLocalizedString("coolgal_passive_description", "this is a cool passive");
                passiveData.dummyPassive = new PassiveAbilityRngBlessing();
                characterData.passive = passiveData;
            }
            
            var stringTable = LocalizationSettings.StringDatabase.GetTable("Main Menu",LocalizationSettings.SelectedLocale);
            Log.LogInfo(LocalizationSettings.SelectedLocale.Identifier.Code);
            Log.LogInfo(stringTable.LocaleIdentifier.Code);
            foreach (var entry in stringTable.m_TableEntries)
            {
                Log.LogInfo(entry.key + " : " + entry.value.Value);
            }
            
            // characterData.passive.name = "cool passive2";
            // characterData.passive.dummyPassive = new PassiveAbilityCurse();
            //
            // characterData.weapon.name = "cool weapon2";
            // characterData.weapon.description = "cool description2";
            
            
            //datamanager.weapons.Add(eWeaponID,characterData.weapon);
            
        }


        public LocalizedString CreateLocalizedString(string uid, string value)
        {
            var stringTable = LocalizationSettings.StringDatabase.GetTable("Main Menu");
            stringTable.AddEntry(uid, value);
            return new LocalizedString(stringTable.TableCollectionName, uid);
            
        }
        
        
    }

    
}