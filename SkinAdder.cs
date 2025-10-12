using Assets.Scripts._Data;
using Assets.Scripts.Saves___Serialization.Progression;
using BepInEx.Logging;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Localization;

namespace CustomCharacterLoader;

public class SkinAdder
{

    public static void AddSkinToGame(JObject jsonObject, AssetBundle assetBundle, CustomCharacterLoaderPlugin.InjectComponent injectComponent, DataManager dataManager, ManualLogSource log)
    {
        var jSkin = JSoloSkin.FromJSON(jsonObject["soloSkin"].Cast<JObject>());
        var skin = ScriptableObject.CreateInstance<SkinData>();
        
        if (dataManager.skinData.TryGetValue((ECharacter)jSkin.eCharacter, out var skinDataList))
        {
            //done first to prevent Garbage collector from removing this object during setup process
            skinDataList.Add(skin);
            dataManager.unsortedSkins.Add(skin);

            skin.character = jSkin.eCharacter;
            skin.name = jSkin.skinName;
            skin.skinType = ESkinType.Default;
            skin.icon = LoadAsset<Texture2D>(jSkin.iconPath,assetBundle);
            var matList = new Il2CppSystem.Collections.Generic.List<Material>();
            foreach(var matPath in jSkin.materialPaths)
            {
                var mat = LoadAsset<Material>(matPath,assetBundle);
                matList.Add(mat);
            }
            skin.materials = matList.ToArray().Cast<Il2CppReferenceArray<Material>>();
            
            skin.localizedName = CreateUniqueLocalizedString(jSkin.eCharacter,jSkin.skinName, jSkin.skinName);
            skin.localizedDescription = CreateUniqueLocalizedString(jSkin.eCharacter,jSkin.skinName + ".description", jSkin.description);
            skin.serializedLocalizationKeysName = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
            skin.serializedLocalizationKeys = new Il2CppSystem.Collections.Generic.List<LocalizationKey>() { };
            
            log.LogInfo("Loaded custom skin: "+jSkin.skinName + " for character "+jSkin.eCharacter);
            //SkinData setup complete. Now add it to skinlist in inject component
            var prefab = LoadAsset<GameObject>(jSkin.prefabPath,assetBundle);
            injectComponent.AddSoloCustomSkin(skin,prefab);

        }
        
        
        
    }
    
    public static T LoadAsset<T>(string assetName, AssetBundle assetBundle) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        return assetBundle.LoadAssetAsync(assetName, Il2CppType.Of<T>()).GetResult().Cast<T>();
    }
    
    public static LocalizedString CreateUniqueLocalizedString(ECharacter eCharacter, string key, string value)
    {
        string eCharStr = "" + eCharacter;
        var uniqueKey = $"{eCharacter}.{key}";
        return CustomCharacterLoaderPlugin.CreateLocalizedString(uniqueKey, value);
    }



    public struct JSoloSkin
    {
        public string skinName; // Name of the skin.
        public string description; // Description of the skin.
        public string iconPath; // Path to the skin's icon.
        public string prefabPath; //path to gameObject
        public ECharacter eCharacter;
        public Il2CppSystem.Collections.Generic.List<string> materialPaths; // List of paths to the materials used for the skin.


        public static JSoloSkin FromJSON(JObject jobj)
        {
            return new JSoloSkin()
            {
                skinName = jobj["skinName"].ToObject<string>(),
                eCharacter = jobj["eCharacter"].ToObject<ECharacter>(),
                description = jobj["description"].ToObject<string>(),
                iconPath = jobj["iconPath"].ToObject<string>(),
                prefabPath = jobj["prefabPath"].ToObject<string>(),
                materialPaths = jobj["materialPaths"].ToObject<Il2CppSystem.Collections.Generic.List<string>>()
            };
        }
    }

    
}