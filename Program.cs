
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using Il2CppSystem.IO;
using UnityEngine;

namespace MyPlugins;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    public static readonly string ASSET_BUNDLE_NAME = "charactermaker";
    public GameObject BepInExUtility;
    public override void Load()
    {
        // var assetPath = Il2CppSystem.IO.Path.Combine(Paths.PluginPath, ASSET_BUNDLE_NAME);
        // var s = Il2CppSystem.IO.File.OpenRead(assetPath);
        // Log.LogInfo(s.name);
        // var mainAssetBundle = UnityEngine.AssetBundle.LoadFromStream(s);
        // Log.LogInfo(mainAssetBundle.name);
        
        Log.LogInfo("Load MyPlugins");
        
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
        //inject.assetBundle = mainAssetBundle;

    }

    public class InjectComponent : MonoBehaviour
    {
        //public AssetBundle assetBundle;
        public ManualLogSource Log;
        public MainMenu menu;
        public CharacterMenu characterMenu;
        
        //needs to be added in IL2CPP to register properly i think
        public InjectComponent(IntPtr handle) : base(handle) { }

        private void Start()
        {
            
        }

        internal void Update()
        {
            GetMenuUI();
            if (Input.GetKeyDown(KeyCode.H))
            {
                Log.LogInfo("Pressed H");
                
                // var myCharacterData = CustomCharacterMaker.LoadAsset(assetBundle, "CharacterData");
                // if (myCharacterData is CharacterData characterData)
                // {
                //     CustomCharacterMaker.AddNewCharacter(characterMenu, characterData);
                // }
                // else
                // {
                //     Log.LogError("Failed to load character data. Isn't of Type CharacterData");
                // }
                var foxData = CustomCharacterMaker.GetFoxData(characterMenu);
                CustomCharacterMaker.AddNewCharacter(characterMenu, foxData);
            }
            //Log.LogInfo("Update");
        }

        internal void GetMenuUI()
        {
            if(!menu)
                menu = FindFirstObjectByType<MainMenu>();
            if(menu && !characterMenu)
                characterMenu = CustomCharacterMaker.CharacterMenuFromUI(menu);
        }
        
        
        
    }

    
}