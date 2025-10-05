
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
        Log.LogInfo("Load MyPlugins");

        var assetPath = Il2CppSystem.IO.Path.Combine(Paths.PluginPath, ASSET_BUNDLE_NAME);
        var s = Il2CppSystem.IO.File.OpenRead(assetPath);
        //Log.LogInfo(s.name);
        var mainAssetBundle = AssetBundle.LoadFromStream(s);
        Log.LogInfo(mainAssetBundle.name);
        
        // AddFileLocatorToAddressables();
        // var settings = TryLoadSettingsAssetAsync();
        // Log.LogInfo(settings);
        //
        // TryLoadAddressableBundle();
        
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
        Log.LogInfo("Finished Loading MyPlugins");

    }
    
    
    private string m_SourceFolder = "dataFiles";

    public void AddFileLocatorToAddressables()
    {
        // if (!Il2CppSystem.IO.Directory.Exists(m_SourceFolder))
        //     return;
        var folderPath = Il2CppSystem.IO.Path.Combine(Paths.PluginPath, m_SourceFolder);
        ResourceLocationMap locator = new ResourceLocationMap(folderPath, 12);
        string providerId = typeof(TextDataProvider).ToString();
        // Log.LogInfo("Looking for files in "+m_SourceFolder);
        string[] files = Il2CppSystem.IO.Directory.GetFiles(folderPath,"*");
        Log.LogInfo("Found " + files.Length + " files in "+folderPath);
        foreach (string filePath in files)
        {
            if (!filePath.EndsWith(".json"))
                continue;
            Il2CppSystem.String keyForLoading = Il2CppSystem.IO.Path.GetFileNameWithoutExtension(filePath);
            var resourceLoc = new ResourceLocationBase(keyForLoading, filePath, providerId, Il2CppType.Of<string>());
            locator.Add(keyForLoading, resourceLoc.Cast<IResourceLocation>() );
            Log.LogInfo("Adding file: " + filePath + "with key: "+keyForLoading);
        }
        Addressables.AddResourceLocator(locator.Cast<IResourceLocator>());
    }
    
    private string m_DataFileName = "settings";

    

    string TryLoadSettingsAssetAsync()
    {
        var loadHandle = Addressables.LoadAssetAsync<string>(m_DataFileName);

        return loadHandle.WaitForCompletion();
        var task = loadHandle.Task;
        return loadHandle.Result;
    }

    private string bundleName = "testbundle.bundle";
    


    public class InjectComponent : MonoBehaviour
    {
        public AssetBundle assetBundle;
        public ManualLogSource Log;
        public MainMenu menu;
        public CharacterMenu characterMenu;
        
        //needs to be added in IL2CPP to register properly i think
        public InjectComponent(IntPtr handle) : base(handle) { }

        private void Start()
        {
            //Activate();
        }

        internal void Update()
        {
            GetMenuUI();
            if (Input.GetKeyDown(KeyCode.H))
            {
                Log.LogInfo("Pressed H");
                Activate();
            }
            //Log.LogInfo("Update");
        }

        void Activate()
        {
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
                
                
                
                
                var foxCharacterData = CustomCharacterMaker.GetFoxData();
                //var ogreCharacter = DataManager.Instance.characterData[ECharacter.Ogre];
                Log.LogInfo(foxCharacterData.weapon.attack.name);
                //realCharacterData.weapon.attack.GetComponent<WeaponAttack>().prefabProjectile = otherCharacter.weapon.attack.GetComponent<WeaponAttack>().prefabProjectile;
                // foxCharacterData.weapon.attack.GetComponent<WeaponAttack>().prefabProjectile = myProjectile;
                // foxCharacterData.prefab = characterData.prefab;

                bool useFoxBaseWeapon = true;
                if (useFoxBaseWeapon)
                {
                    var tempWeapon = weaponData;
                    tempWeapon.upgradeData = ScriptableObject.CreateInstance<UpgradeData>();
                    tempWeapon.upgradeData.upgradeModifiers = foxCharacterData.weapon.upgradeData.upgradeModifiers;
                    tempWeapon.baseStats = new Il2CppSystem.Collections.Generic.Dictionary<EStat, float>();
                    tempWeapon.baseStats.Add(EStat.AttackSpeed,2f);
                    tempWeapon.baseStats.Add(EStat.Projectiles,2f);
                    tempWeapon.baseStats.Add(EStat.SizeMultiplier,2f);
                    tempWeapon.baseStats.Add(EStat.ProjectileBounces,1f);
                    tempWeapon.baseStats.Add(EStat.DurationMultiplier,1f);
                    tempWeapon.baseStats.Add(EStat.ProjectileSpeedMultiplier,1f);
                    tempWeapon.baseStats.Add(EStat.KnockbackMultiplier,1f);
                    tempWeapon.baseStats.Add(EStat.CritChance,1f);
                    tempWeapon.baseStats.Add(EStat.DamageMultiplier,1f);
                    tempWeapon.baseStats.Add(EStat.CritDamage,1f);

                    tempWeapon.damageSourceName = "Test Weapon";
                    tempWeapon.attackDuration = 1;
                    tempWeapon.spawnProjectileRange = 40;
                    //weaponData = tempWeapon;

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
                var character = DataManager.Instance.GetCharacterData((ECharacter)2001);
                Log.LogInfo(character.name);
                CustomCharacterMaker.AddNewCharacterToButtonMenu(characterMenu, characterData);
  
        }

        internal void GetMenuUI()
        {
            if(!menu)
                menu = FindFirstObjectByType<MainMenu>();
            if(menu && !characterMenu)
                characterMenu = CustomCharacterMaker.CharacterMenuFromUI(menu);
        }

        public static T CopyComponent<T>(GameObject destination, T original) where T : Component
        {
            var type = original.GetIl2CppType();
            Component copy = destination.AddComponent(type);
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    property.SetValue(copy, property.GetValue(original, null), null);
                }
            }
            return copy as T;
        }
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
            

            // characterData.passive.name = "cool passive2";
            // characterData.passive.dummyPassive = new PassiveAbilityCurse();
            //
            // characterData.weapon.name = "cool weapon2";
            // characterData.weapon.description = "cool description2";
            
            
            //datamanager.weapons.Add(eWeaponID,characterData.weapon);
            
        }


        public LocalizedString CreateLocalizedString(string uid, string value)
        {
            var stringTable = LocalizationSettings.StringDatabase.GetTable("Main Menu",LocalizationSettings.SelectedLocale);
            Log.LogInfo(stringTable.name);
            var entry = stringTable.AddEntry(uid,"en");
            entry.Value = value;
            stringTable.GetEntry(uid);
            var lstring = new LocalizedString(stringTable.name, uid);
            lstring.FallbackState = FallbackBehavior.UseFallback;
            return lstring;
        }
        
        
    }

    
}