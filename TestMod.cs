// using UnityEngine;
// using MelonLoader;
//
// // [assembly: MelonInfo(typeof(TimeFreezer.TimeFreezerMod), "Time Freezer", "1.0.0", "SlidyDev")]
//
// namespace TimeFreezer
// {
//     public class TimeFreezerMod : MelonMod
//     {
//         private static KeyCode freezeToggleKey;
//         
//         private static bool frozen;
//         private static float baseTimeScale;
//
//         public override void OnEarlyInitializeMelon()
//         {
//             freezeToggleKey = KeyCode.Space;
//         }
//
//         public override void OnLateUpdate()
//         {
//             if (Input.GetKeyDown(freezeToggleKey))
//             {
//                 ToggleFreeze();
//             }
//         }
//         
//         public static void DrawFrozenText()
//         {
//             GUI.Label(new Rect(20, 20, 1000, 200), "<b><color=cyan><size=100>Frozen</size></color></b>");
//         }
//
//         private static void ToggleFreeze()
//         {
//             frozen = !frozen;
//
//             if (frozen)
//             {
//                 Melon<TimeFreezerMod>.Logger.Msg("Freezing");
//                 
//                 MelonEvents.OnGUI.Subscribe(DrawFrozenText, 100); // Register the 'Frozen' label
//                 baseTimeScale = Time.timeScale; // Save the original time scale before freezing
//                 Time.timeScale = 0;
//             }
//             else
//             {
//                 Melon<TimeFreezerMod>.Logger.Msg("Unfreezing");
//                 
//                 MelonEvents.OnGUI.Unsubscribe(DrawFrozenText); // Unregister the 'Frozen' label
//                 Time.timeScale = baseTimeScale; // Reset the time scale to what it was before we froze the time
//             }
//         }
//
//         public override void OnDeinitializeMelon()
//         {
//             if (frozen)
//             {
//                 ToggleFreeze(); // Unfreeze the game in case the melon gets unregistered
//             }
//         }
//     }
// }