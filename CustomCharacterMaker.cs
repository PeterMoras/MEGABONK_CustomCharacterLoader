
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

    public static void AddNewCharacter(CharacterMenu menu, CharacterData character)
    {
        var baseCharBox = menu.characterPrefabUi;
        var parent = baseCharBox.transform.parent;
        
        var newCharBox = GameObject.Instantiate(baseCharBox, parent);
        var charData = newCharBox.GetComponent<MyButtonCharacter>();
        charData.SetCharacter(character);
    }

    public static CharacterData GetFoxData(CharacterMenu menu)
    {
        var baseCharBox = menu.characterPrefabUi;
        return baseCharBox.GetComponent<MyButtonCharacter>().characterData;
    }
    // public static UnityEngine.Object LoadAsset(AssetBundle assetBundle, string assetName)
    // {
    //     return UnityEngine.Object.Instantiate<GameObject>(assetBundle.LoadAsset(assetName,Il2CppType.Of<GameObject>()).Cast<GameObject>());
    //     
    // }
}