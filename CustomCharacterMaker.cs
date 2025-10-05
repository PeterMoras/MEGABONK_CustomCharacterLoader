
using Il2CppInterop.Runtime;
using UnityEngine;

namespace MyPlugins;

public class CustomCharacterMaker
{
    //Character Menu Path:
    // UI > Tabs > Character > W_Character
    // W_Character > W_indowLayers > Content > ScrollRect > CharacterGrid > CharacterPrefabUI
    //W_Character.CharacterMenu class for managing characters
    //CharacterPrefabUI.MyButtonCharacter for data on selectable character
    // MyButtonCharacter.characterData and .data for storing character specific info
    // use .SetCharacter(CharacterData) to update
    // UI.MainMenu class

    public static CharacterMenu CharacterMenuFromUI(MainMenu mainMenu)
    {
        return mainMenu.tabCharacters.transform.GetChild(0).GetComponent<CharacterMenu>();
    }

    public static void AddNewCharacterToButtonMenu(CharacterMenu menu, CharacterData character)
    {
        var baseCharBox = menu.characterPrefabUi;
        var parent = baseCharBox.transform.parent;
        
        var newCharBox = UnityEngine.Object.Instantiate(baseCharBox, parent);
        var charData = newCharBox.GetComponent<MyButtonCharacter>();
        charData.SetCharacter(character);
    }

    public static CharacterData GetFoxData()
    {
        return DataManager.Instance.characterData[ECharacter.Fox];
        // var baseCharBox = menu.characterPrefabUi;
        // return baseCharBox.GetComponent<MyButtonCharacter>().characterData;
    }
    public static UnityEngine.Object LoadAsset(AssetBundle assetBundle, string assetName)
    {
        var asset = assetBundle.LoadAssetAsync(assetName, Il2CppType.Of<UnityEngine.Object>());
        return UnityEngine.Object.Instantiate<UnityEngine.Object>(asset.GetResult());
        
    }
    public static GameObject importModel(AssetBundle assetBundle,string assetName)
    {

        //assetBundle.LoadAllAssets(Il2CppType.Of<GameObject>());
        //Log.LogInfo("Loaded all assets of type: "+Il2CppType.Of<GameObject>());
        //MainAssetBundle.LoadAllAssets<UnityEngine.GameObject>();
        //Object.
        return UnityEngine.Object.Instantiate<GameObject>(assetBundle.LoadAsset(assetName,Il2CppType.Of<GameObject>()).Cast<GameObject>());
        
    }
}