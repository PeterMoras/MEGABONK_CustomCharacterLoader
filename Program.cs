

using CustomCharacterLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Actors.Player;
using Il2CppAssets.Scripts.Saves___Serialization.Progression.Achievements;
using Il2CppAssets.Scripts.Saves___Serialization.Progression.Stats;
using Il2CppAssets.Scripts.Saves___Serialization.Progression.Unlocks;
using Il2CppAssets.Scripts.UI.Localization;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Collections;
using Il2CppSystem.IO;
using Il2CppSystem.Reflection;
using Il2CppSystem.Runtime.InteropServices;
using JigglePhysics;
using MelonLoader;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.Rendering;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Serialization;
using Directory = System.IO.Directory;
using EnumerationOptions = Il2CppSystem.IO.EnumerationOptions;
using File = Il2CppSystem.IO.File;
using Input = UnityEngine.Input;
using KeyCode = UnityEngine.KeyCode;
using Object = UnityEngine.Object;
using Path = Il2CppSystem.IO.Path;
using SearchOption = System.IO.SearchOption;
using StreamReader = Il2CppSystem.IO.StreamReader;

[assembly: MelonInfo(typeof(CustomCharacterLoaderPlugin), "CustomCharacterLoader", "1.4.0", "PeterM")]
[assembly: MelonGame("Ved", "Megabonk")]
[assembly: MelonColor(255,32,32,200)]

namespace CustomCharacterLoader;
public class CustomCharacterLoaderPlugin : MelonMod
{
    public static readonly string CUSTOM_CHARACTER_FOLDER = "CustomCharacters";
    public static GameObject MelonUtility;
    public override void OnInitializeMelon()
    {
        // Melon<CustomCharacterLoaderPlugin>.Logger.Msg("OnInitializeMelon");
        var customCharacterPath = Il2CppSystem.IO.Path.Combine("Mods", CUSTOM_CHARACTER_FOLDER);
        if (!Directory.Exists(customCharacterPath) && customCharacterPath != null)
            Directory.CreateDirectory(customCharacterPath);
        
        ClassInjector.RegisterTypeInIl2Cpp<InjectComponent>();
        ClassInjector.RegisterTypeInIl2Cpp<JiggleRigBuilder>();
        MelonUtility = GameObject.Find("MelonUtility");
        
        if (MelonUtility == null)
        {
            MelonUtility = new GameObject("MelonUtility");
            GameObject.DontDestroyOnLoad(MelonUtility);
            MelonUtility.hideFlags = HideFlags.HideAndDontSave;
            MelonUtility.AddComponent<InjectComponent>();
        }
        else MelonUtility.AddComponent<InjectComponent>();
        var inject = MelonUtility.GetComponent<InjectComponent>();
        //inject.assetBundle = mainAssetBundle;
        InjectComponent.Instance = inject;
        
        // HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("CustomCharacterLoader");
        // harmony.PatchAll();

    }


    [HarmonyPatch()]
    public static class LoadCorrectSkinDataPatch
    {
        [HarmonyPatch(typeof(DataManager),nameof(DataManager.GetSkin))]
        [HarmonyPrefix]
        internal static bool Prefix(DataManager __instance, ECharacter character, int savedIndex,ref SkinData __result)
        {
            var skins = __instance.GetSkins(character);
            // InjectComponent.Instance.Log.LogInfo($"skin data: index - {savedIndex} | count - {skins.Count}");

            if (savedIndex >= 0 && savedIndex < skins.Count)
            {
                __result = skins._items[savedIndex];
            }
            else
            {
                if (skins.Count <= 0) __result = null;
                else __result = skins._items[0];
            }

            return false;
        }

        [HarmonyPatch(typeof(PlayerRenderer), nameof(PlayerRenderer.CreateMaterials))]
        [HarmonyPostfix]
        internal static void Postfix(PlayerRenderer __instance, int amount)
        {
            if(__instance.skinData == null) return;
            var characterData = __instance.characterData;
            bool isCustom = !String.IsNullOrEmpty(__instance.skinData.author);
            var log = Melon<CustomCharacterLoaderPlugin>.Logger;
            var defaultMaterial = InjectComponent.Instance.DefaultMaterial;
            
        
            if (isCustom)
            {
                
                var materials = __instance.skinData.materials;
                var finalMaterials = new Material[(long)materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = new Material(materials[i]);
                    if (material.shader.name.EndsWith("MegabonkShader"))
                    {
                        material = defaultMaterial;
                    }
                    finalMaterials[i] = material;
                }
                __instance.activeMaterials = finalMaterials;
                var matList = new Il2CppSystem.Collections.Generic.List<Material>();
                foreach (var mat in finalMaterials)
                {
                    __instance.allMaterials.Add(mat);
                    // log.LogInfo(mat.name);
                }
                __instance.allMaterials = matList;
                __instance.renderer.materials = finalMaterials;
        
            }
            else
            {
                // if(__instance.skinData?.materials != null)
                //     __instance.activeMaterials = __instance.skinData.materials;
            }
        
        }
    }
    [HarmonyPatch()]
    public static class ActivationTogglePatches
    {
        [HarmonyPatch(typeof(MyAchievements), nameof(MyAchievements.IsActivated))]
        [HarmonyPrefix]
        private static bool IsActivatedPrefix(ref bool __result, UnlockableBase unlockable)
        {
            __result = !SaveManager.Instance.progression.inactivated.Contains(unlockable.GetInternalName());
            return false; // stop original
        }
        [HarmonyPatch(typeof(MyAchievements), nameof(MyAchievements.IsAvailable))]
        [HarmonyPrefix]
        private static bool IsAvailablePrefix(ref bool __result, UnlockableBase unlockable)
        {
            __result = MyAchievements.IsActivated(unlockable) && MyAchievements.IsPurchased(unlockable);
            return false; // stop original
        }
        
        
        [HarmonyPatch(typeof(MyAchievements),nameof(MyAchievements.CanToggleActivation))]
        [HarmonyPrefix]
        internal static bool Prefix(DataManager __instance, UnlockableBase unlockable, ref bool __result)
        {
            bool isBaseWeapon = String.IsNullOrEmpty(unlockable.author);
            if (!isBaseWeapon)
            {
                __result = true;
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch()]
    public static class ShowCorrectCustomDamageSourcePatch
    {
        [HarmonyPatch(typeof(LocalizationUtility), nameof(LocalizationUtility.GetLocalizedDamageSource))]
        [HarmonyPrefix]
        internal static bool Prefix(LocalizationUtility __instance, ref string __result, string source)
        {
            //is custom weapon?
            if (InjectComponent.Instance.CustomWeapons.TryGetValue(source, out var customWeapon))
            {
                __result = customWeapon.name;
                return false;
            }

            return true;
        }
        [HarmonyPatch(typeof(DamageSource), nameof(DamageSource.GetIcon))]
        [HarmonyPrefix]
        internal static bool Prefix(DamageSource __instance, ref Texture __result)
        {
            //is custom weapon?
            if (InjectComponent.Instance.CustomWeapons.TryGetValue(__instance.damageSource, out var customWeapon))
            {
                __result = customWeapon.icon;
                return false;
            }

            return true;
        }
    }
  
    

    [HarmonyPatch(typeof(DataManager))]
    [HarmonyPatch(nameof(DataManager.Load))]
    public static class DataManagerLoadCustomCharacterPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(DataManager __instance)
        {
            //Debug.Log("Trigger Load Custom Character Data");
            //InjectComponent.Instance.InitCustomCharacter();
            InjectComponent.Instance.LoadCustomCreations();
        }
    }
    [HarmonyPatch(typeof(SaveManager),nameof(SaveManager.Load),new Type[] { typeof(bool) })]
    public static class SaveManagerLoadCustomCharacterPatch
    {
        [HarmonyPostfix]
        internal static void Postfix(SaveManager __instance, bool loadBackup)
        {
            Debug.Log("Patching Save File with Custom Character Skin data");
            //InjectComponent.Instance.InitCustomCharacter();
            var characters = InjectComponent.Instance.AddedCharacters;
            foreach (var eCharacter in characters)
            {
                __instance.config.preferences.characterSkins.TryAdd(eCharacter,0);
            }
        }
    }
    [HarmonyPatch()]
    public static class PlayerRendererCustomSkinLoaderPatch
    {
        [HarmonyPatch(typeof(PlayerRenderer), nameof(PlayerRenderer.SetCharacter))]
        [HarmonyPostfix]
        internal static void Postfix(PlayerRenderer __instance, CharacterData characterData,
            PlayerInventory inventory,
            Vector3 spawnDir)
        {
            var oldPhysBones = characterData.prefab.GetComponentsInChildren<MyPhysicsBone>();
            var newPhysBones = __instance.rendererObject.GetComponentsInChildren<MyPhysicsBone>();
            for (int i = 0; i < oldPhysBones.Length; i++)
            {
                var oldBone = oldPhysBones[i];
                var newBone = newPhysBones[i];
                MyPhysicsBone.CopyValuesTo(oldBone,newBone);
                newBone.enabled = false;
            }
            var oldJiggleRigs = characterData.prefab.GetComponentsInChildren<JiggleRigBuilder>();
            var newJiggleRigs = __instance.rendererObject.GetComponentsInChildren<JiggleRigBuilder>();
            PhysBoneAdder.CopyRigsValues(__instance.rendererObject, oldJiggleRigs, newJiggleRigs);
            

            // var rig = __instance.rendererObject.AddComponent<JiggleRigBuilder>();
            // rig.rootTransform = __instance.rendererObject.transform;
            // rig.Initialize();
            // rig.jiggleSettings = ScriptableObject.CreateInstance<JiggleSettings>();

        }
        
        
        [HarmonyPatch(typeof(PlayerRenderer),nameof(PlayerRenderer.SetSkin),new Type[] { typeof(SkinData) })]
        [HarmonyPrefix]
        internal static bool Prefix(PlayerRenderer __instance, SkinData skinData)
        {
            var gameObject = InjectComponent.Instance.GetModelFromSkinData(skinData);
            //InjectComponent.Instance.Log.LogInfo("SkinData: "+skinData?.name);
            if (gameObject == null)
            {
                // InjectComponent.Instance.Log.LogInfo("Unable to find game object for "+skinData.name);
                return true;
            }
            UpdatePlayerRendererWithNewGameObject(gameObject, __instance, skinData);
            
            return true; //never skip
        }

        internal static void UpdatePlayerRendererWithNewGameObject(GameObject prefab, PlayerRenderer pRenderer, SkinData skinData)
        {
            var log = Melon<CustomCharacterLoaderPlugin>.Logger;

            var newMesh =  prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var originalMesh = pRenderer.renderer;
            var originalGameObject = pRenderer.rendererObject;
            if (newMesh.sharedMesh == originalMesh.sharedMesh)
            {
                // InjectComponent.Instance.Log.LogInfo("Same Mesh as original, dont update");

                return;
            }

            var instancedPrefab = GameObject.Instantiate(prefab, pRenderer.transform);
            instancedPrefab.transform.localPosition = Vector3.zero;
            
            var oldPhysBones = prefab.GetComponentsInChildren<MyPhysicsBone>();
            var newPhysBones = instancedPrefab.GetComponentsInChildren<MyPhysicsBone>();
            for (int i = 0; i < oldPhysBones.Length; i++)
            {
                var oldBone = oldPhysBones[i];
                var newBone = newPhysBones[i];
                MyPhysicsBone.CopyValuesTo(oldBone,newBone);
            }
            var oldJiggleRigs = prefab.GetComponentsInChildren<JiggleRigBuilder>();
            var newJiggleRigs = instancedPrefab.GetComponentsInChildren<JiggleRigBuilder>();
            PhysBoneAdder.CopyRigsValues(instancedPrefab,oldJiggleRigs, newJiggleRigs);

            //update values 
            var skins = DataManager.Instance.skinData[skinData.character];
            SkinData tempSkinToUse = skinData;

            pRenderer.skinData = tempSkinToUse;
            pRenderer.rendererObject = instancedPrefab;
            pRenderer.renderer = newMesh;
            pRenderer.hips = newMesh.rootBone;
            pRenderer.animator = instancedPrefab.GetComponent<Animator>();
            pRenderer.torso = null;
            var materials = new Material[skinData.materials.Length];
            var matList = new Il2CppSystem.Collections.Generic.List<Material>(materials.Length);

            for (int i = 0; i < skinData.materials.Length; i++)
            {
                var newMat = new Material(skinData.materials[i]);
                materials[i] = newMat;
                matList.Add(newMat);
            }
            pRenderer.activeMaterials = materials;
             foreach (var m in materials)
                 matList.Add(m);
            pRenderer.allMaterials = matList;
            pRenderer.renderer.materials = materials;
            
            GameObject.Destroy(originalGameObject);
            
            //update CharacterData with original gameobject reference so it works in game
            pRenderer.characterData.prefab = prefab;

        }

    }

    public static CustomType DetermineCustomType(JObject jsonObject)
    {
        if (jsonObject.ContainsKey("character")) return CustomType.Character;
        if (jsonObject.ContainsKey("soloSkin")) return CustomType.Skin;
        if (jsonObject.ContainsKey("soloWeapon")) return CustomType.Weapon;
        
        return CustomType.Character;
    }
    
    public static LocalizedString CreateLocalizedString(string uid, string value)
    {
        var stringTable = LocalizationSettings.StringDatabase.GetTable("Main Menu");
        stringTable.AddEntry(uid, value);
        return new LocalizedString(stringTable.TableCollectionName, uid);
    }
    

    public class InjectComponent : MonoBehaviour
    {
        public static InjectComponent Instance;
        public List<ECharacter> AddedCharacters;
        public Material DefaultMaterial;
        public Dictionary<string, WeaponData> CustomWeapons = new Dictionary<string, WeaponData>();
        public Window gcHolder;
        public Animator scriptGcHolder;

        public Dictionary<SkinData, GameObject> SkinRenderObjects = new Dictionary<SkinData, GameObject>();
        
        // public AssetBundle assetBundle;
        // public MainMenu menu;
        // public CharacterMenu characterMenu;
        
        //needs to be added in IL2CPP to register properly I think
        public InjectComponent(IntPtr handle) : base(handle) { }

        void Update()
        {

            if (UnityEngine.Input.GetKeyDown(KeyCode.H))
            {
                //var animatorController = DataManager.Instance.characterData[ECharacter.Fox]?.prefab?.GetComponent<Animator>();
                //AnalyzeShaderProperties();
            }
        }

        public void AddCustomWeapon(WeaponData weapon)
        {
            CustomWeapons.Add(weapon.damageSourceName, weapon);
        }

        private void AnalyzeShaderProperties()
        {
            var playerRenderer = MyPlayer.Instance.playerRenderer;
            var shader = playerRenderer.activeMaterials[0]?.shader;
            if (shader != null)
            {
                var count = shader.GetPropertyCount();
                for (int i = 0; i < count; i++)
                {
                    var name = shader.GetPropertyName(i);
                    var type = shader.GetPropertyType(i);
                    if (type == ShaderPropertyType.Float)
                    {
                        var value = shader.GetPropertyDefaultFloatValue(i);
                        Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", {type}) = {value}");
                    }
                    else if (type == ShaderPropertyType.Range)
                    {
                        var value = shader.GetPropertyDefaultFloatValue(i);
                        var range = shader.GetPropertyRangeLimits(i);
                        Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", Range({range[0]},{range[1]})) = {value}");
                    }else if (type == ShaderPropertyType.Vector)
                    {
                        var value = shader.GetPropertyDefaultVectorValue(i);
                        var valueStr = $"({value[0]},{value[1]},{value[2]},{value[3]})";
                        Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", Vector) = {valueStr}");
                    }else if (type == ShaderPropertyType.Color)
                    {
                        var value = shader.GetPropertyDefaultVectorValue(i);
                        var valueStr = $"({value[0]},{value[1]},{value[2]},{value[3]})";
                        Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", Color) = {valueStr}");
                    }else if (type == ShaderPropertyType.Texture)
                    {
                        var dimension = shader.GetPropertyTextureDimension(i);
                        var defaultName = shader.GetPropertyTextureDefaultName(i);
                        if (dimension == TextureDimension.Tex2D)
                        {
                            Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", 2D) = \"{defaultName}\" {{}}");

                        }
                        else
                        {
                            Melon<CustomCharacterLoaderPlugin>.Logger.Msg($"{name} (\"{name}\", {dimension})");
                        }
                    }
                        
                }
            }
        }


        public void LoadCustomCreations()
        {
            
            var dataManager = DataManager.Instance;
            var paths = FindCustomCharacterPaths();
            AddedCharacters = new List<ECharacter>();

            //Get default material
            DefaultMaterial = dataManager.skinData[ECharacter.Fox]._items[0].materials[0];
            
            //Setup custom skin manager
            SetupCustomSkinLoader(dataManager);
            
            Melon<CustomCharacterLoaderPlugin>.Logger.Msg("Loading Custom Creations");
            foreach (var jsonPath in paths)
            {
                int endPos = jsonPath.LastIndexOf('.');
                if(jsonPath.EndsWith(".custom.json"))
                    endPos = jsonPath.LastIndexOf(".custom.json");
                string assetPath = jsonPath.Substring(0, endPos);
                var s = Il2CppSystem.IO.File.OpenRead(assetPath);
                var assetBundle = AssetBundle.LoadFromStream(s);
                s.Dispose();
                var s2 = Il2CppSystem.IO.File.OpenRead(jsonPath);
                var steamReader = new StreamReader(s2);
                var jsonObject = JObject.Parse(steamReader.ReadToEnd());
                
                steamReader.Dispose();
                s2.Dispose();
                var customType = DetermineCustomType(jsonObject);

                switch (customType)
                {
                    case CustomType.Character:
                        var reader = new CharacterAdder(dataManager, jsonObject, assetBundle, Melon<CustomCharacterLoaderPlugin>.Logger);
                        var eCharacter = reader.AddCustomCharacter();
                        AddedCharacters.Add(eCharacter);
                        break;
                    case CustomType.Skin:
                        SkinAdder.AddSkinToGame(jsonObject, assetBundle, this, dataManager, Melon<CustomCharacterLoaderPlugin>.Logger);
                        break;
                    default:
                        break;
                }
                
                
            }
        }
        
        public GameObject GetModelFromSkinData(SkinData skinData)
        {
            SkinRenderObjects.TryGetValue(skinData, out var renderObject);
            return renderObject;
        }

        public void AddSoloCustomSkin(SkinData skinData, GameObject prefab)
        {
            SkinRenderObjects.Add(skinData, prefab);
            var fakeCharacterData = ScriptableObject.CreateInstance<CharacterData>();
            fakeCharacterData.prefab = prefab;
            this.GetComponent<DataManager>().unsortedCharacterData.Add(fakeCharacterData);
            //prefab.transform.parent = this.transform;
        }
        
        
        
        public void SetupCustomSkinLoader(DataManager dataManager)
        {
            //creates a lookup between all base skins and their render gameobjects for the PlayerRenderer component
            var allBaseSkins = dataManager.skinData;
            HashSet<CharacterData> uniqueCharacterSkins = new HashSet<CharacterData>();
            
            foreach (var characterSkinListEntry in allBaseSkins)
            {
                if (characterSkinListEntry.value.Count == 0) continue;
                
                ECharacter character = characterSkinListEntry.value._items[0].character;
                var skinList = characterSkinListEntry.value;
                var success = dataManager.characterData.TryGetValue(character,out var characterData);
                
                //Log.LogInfo($"get key {character} for skinlist {characterData}");
                
                if (success)
                {
                    foreach (var skin in skinList)
                    {
                        SkinRenderObjects.Add(skin,characterData.prefab);
                        uniqueCharacterSkins.Add(characterData);
                    }
                }
            }
            //add a reference to all unique skins so they dont unload
            var myDm = this.gameObject.AddComponent<DataManager>();
            myDm.unsortedCharacterData = new Il2CppSystem.Collections.Generic.List<CharacterData>();

            foreach (var skin in uniqueCharacterSkins)
            {
                myDm.unsortedCharacterData.Add(Instantiate(skin)); //copy of clone so no alteratiosn affect the reference
            }
            
            
        }
        
        public static string[] FindCustomCharacterPaths()
        {
            var customCharacterPath = Il2CppSystem.IO.Path.Combine("Mods", CUSTOM_CHARACTER_FOLDER);
            var assetPaths = Il2CppSystem.IO.Directory.GetFiles(customCharacterPath, "*.json").ToHashSet();
            var additionalCharacters = Il2CppSystem.IO.Directory.GetFiles("./", "*.custom.json", new EnumerationOptions(){ RecurseSubdirectories = true });
            assetPaths.UnionWith(additionalCharacters);
            return assetPaths.ToArray();
        }
   
        
    }

    public enum CustomType
    {
        Character,
        Skin,
        Weapon
    }
    
    public class MyRefTest : MonoBehaviour
    {
        public List<GameObject> testObjects = new List<GameObject>();
    }

    
}