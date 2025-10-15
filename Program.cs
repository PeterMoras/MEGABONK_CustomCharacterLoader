
using CustomCharacterLoader;
using HarmonyLib;
using Il2Cpp;
using Il2CppAssets.Scripts.Actors.Player;
using Il2CppAssets.Scripts.Saves___Serialization.Progression.Achievements;
using Il2CppAssets.Scripts.Saves___Serialization.Progression.Unlocks;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Collections;
using Il2CppSystem.IO;
using Il2CppSystem.Reflection;
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

[assembly: MelonInfo(typeof(CustomCharacterLoaderPlugin), "CustomCharacterLoader", "1.2.0", "PeterM")]
 [assembly: MelonGame("Ved", "Megabonk")]
 [assembly: MelonColor(255,32,32,200)]

namespace CustomCharacterLoader;
public class CustomCharacterLoaderPlugin : MelonMod
{
    public static readonly string CUSTOM_CHARACTER_FOLDER = "CustomCharacters";
    public static GameObject MelonUtility;
    public override void OnInitializeMelon()
    {
        MelonLogger.Msg("OnInitializeMelon");
        MelonLogger.Msg(Directory.GetCurrentDirectory());
         var customCharacterPath = Il2CppSystem.IO.Path.Combine("Mods", CUSTOM_CHARACTER_FOLDER);
         MelonLogger.Msg(customCharacterPath);
         if (!Directory.Exists(customCharacterPath) && customCharacterPath != null)
             Directory.CreateDirectory(customCharacterPath);
        
         ClassInjector.RegisterTypeInIl2Cpp<InjectComponent>();
         ClassInjector.RegisterTypeInIl2Cpp<MyRefTest>();
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
            if (savedIndex >= 0 && savedIndex < skins.Count)
            {
                __result = skins._items[savedIndex];
            }
            else
            {
                __result = skins._items[0];
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
                var finalMaterials = new Il2CppReferenceArray<Material>((long)materials.Length);
                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
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
                    log.Msg(mat.name);
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
    [HarmonyPatch(typeof(PlayerRenderer),nameof(PlayerRenderer.SetSkin),new Type[] { typeof(SkinData) })]
    public static class PlayerRendererCustomSkinLoaderPatch
    {
        [HarmonyPrefix]
        internal static bool Prefix(PlayerRenderer __instance, SkinData skinData)
        {
            var gameObject = InjectComponent.Instance.GetModelFromSkinData(skinData);
            if (gameObject == null)
            {
                Melon<CustomCharacterLoaderPlugin>.Logger.Msg("Unable to find game object for "+skinData.name);
                return true;
            }
            UpdatePlayerRendererWithNewGameObject(gameObject, __instance);
            
            return true; //never skip
        }

        internal static void UpdatePlayerRendererWithNewGameObject(GameObject prefab, PlayerRenderer pRenderer)
        {


            var newMesh =  prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            var originalMesh = pRenderer.renderer;
            var originalGameObject = pRenderer.rendererObject;
            if (newMesh.sharedMesh == originalMesh.sharedMesh)
            {
                //InjectComponent.Instance.Log.LogInfo("Same Mesh as original, dont update");
                return;
            }

            var instancedPrefab = GameObject.Instantiate(prefab, pRenderer.transform);
            instancedPrefab.transform.localPosition = Vector3.zero;
            newMesh =  prefab.GetComponentInChildren<SkinnedMeshRenderer>();
            //update values 
            pRenderer.rendererObject = instancedPrefab;
            pRenderer.renderer = newMesh;
            pRenderer.hips = newMesh.rootBone;
            pRenderer.animator = instancedPrefab.GetComponent<Animator>();
            pRenderer.torso = null;
            //pRenderer.activeMaterials = pRenderer.renderer.materials;
            var matList = new Il2CppSystem.Collections.Generic.List<Material>(pRenderer.renderer.materials.Count);
            foreach (var m in pRenderer.renderer.materials)
                matList.Add(m);
            //pRenderer.allMaterials = matList;
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
        //public ManualLogSource Log;
        public List<ECharacter> AddedCharacters;
        public Material DefaultMaterial;

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
            MelonLogger.Msg(Directory.GetCurrentDirectory());
            
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
                        var reader = new CharacterAdder(dataManager, jsonObject, assetBundle);
                        var eCharacter = reader.AddCustomCharacter();
                        AddedCharacters.Add(eCharacter);
                        break;
                    case CustomType.Skin:
                        SkinAdder.AddSkinToGame(jsonObject, assetBundle, this, dataManager);
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
            var customCharacterPath = Path.Combine("Mods", CUSTOM_CHARACTER_FOLDER);
            string[] assetPaths = Il2CppSystem.IO.Directory.GetFiles(customCharacterPath, "*.json");

            string[] additionalCharacters = Il2CppSystem.IO.Directory.GetFiles("Mods", "*.custom.json", new EnumerationOptions(){ RecurseSubdirectories = true });
            
            return assetPaths.Concat(additionalCharacters).ToArray();
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