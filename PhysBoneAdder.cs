using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppSystem.Runtime.InteropServices;
using JigglePhysics;
using MelonLoader;
using UnityEngine;
using UnityEngine.ProBuilder;
using Object = System.Object;

namespace CustomCharacterLoader;

public class PhysBoneAdder
{
    public static void SetBonesOnPrefab(GameObject prefab, JArray boneJson)
    {
        var log = Melon<CustomCharacterLoaderPlugin>.Logger;
        //log.Msg($"Loading {boneJson._values.Count} Phys Bones");

        var existingPhysBones = prefab.GetComponentsInChildren<MyPhysicsBone>();
        
        foreach (var jBone in boneJson._values)
        {
            //convert json into intermediate object
            var jPhysBone = JPhysBone.FromJson(jBone.Cast<JObject>());  
            // find correct transform
            var physBone = existingPhysBones.First((b) => b.name == jPhysBone.name);
            //log.LogInfo($"bone stats: {physBone.damping} | {physBone.elasticity} | {physBone.stiffness}");

            physBone.damping = jPhysBone.damping;
            physBone.gravity = jPhysBone.gravity;
            physBone.elasticity = jPhysBone.elasticity;
            physBone.stiffness = jPhysBone.stiffness;
            physBone.inert = jPhysBone.inert;
            physBone.stiffnessCurve = jPhysBone.stiffnessCurve;
            physBone.dampingCurve = jPhysBone.dampingCurve;
            physBone.elasticityCurve = jPhysBone.elasticityCurve;
            physBone.inertCurve = jPhysBone.inertCurve;
            //log.LogDebug($"updated bone {physBone.name}");
            //log.LogInfo($"bone stats: {physBone.damping} | {physBone.elasticity} | {physBone.stiffness} | {physBone.inert}");

        }
    }
    public static void SetJiggleOnPrefab(GameObject prefab, JArray jiggleBones)
    {
        var log = Melon<CustomCharacterLoaderPlugin>.Logger;
        //log.Msg($"Loading {jiggleBones._values.Count} Jiggle Bones");
        var allColliders = prefab.GetComponentsInChildren<Collider>();
        var existingJiggleRigs = prefab.GetComponentsInChildren<JiggleRigBuilder>().ToList();
        //log.Msg($"Found {existingJiggleRigs.Count} Rigs for prefab's children {prefab.name}");
        
        foreach (var jJiggleRig in jiggleBones._values)
        {
            //var rig = prefab.AddComponent<JiggleRigBuilder>();
            var jiggleData = JJiggleBone.FromJson(jJiggleRig.Cast<JObject>());
            var rig = existingJiggleRigs.First(r => r.name == jiggleData.name);
            existingJiggleRigs.Remove(rig); //don't accidentally double up
            rig.rootTransform = prefab.GetComponentsInChildren<Transform>().First(tf => tf.name == jiggleData.rootName);

            rig.wind = jiggleData.wind;
            rig.colliders = new List<Collider>();
            foreach (var colliderName in jiggleData.colliders)
            {
                var collider = allColliders.First(c => c.name == colliderName);
                rig.colliders.Add(collider);
            }
            var settings = new JiggleSettingsData();
            //CustomCharacterLoaderPlugin.InjectComponent.Instance.PreventGCCleanup(settings);
            settings.gravityMultiplier = jiggleData.data.gravityMultiplier;
            settings.friction = jiggleData.data.friction;
            settings.angleElasticity = jiggleData.data.angleElasticity;
            settings.blend = jiggleData.data.blend;
            settings.airDrag = jiggleData.data.airDrag;
            settings.lengthElasticity = jiggleData.data.lengthElasticity;
            settings.elasticitySoften = jiggleData.data.elasticitySoften;
            settings.radiusMultiplier = jiggleData.data.radiusMultiplier;
            settings.radiusCurve = jiggleData.data.radiusCurve;
            rig.data = settings;
            rig.Initialize();
        }
    }

    public struct JJiggleBone
    {
        public string name;
        public string rootName;
        public Vector3 wind;
        public string[] colliders;
        public JJiggleData data;


        public struct JJiggleData
        {
            public float gravityMultiplier;
            public float friction;
            public float angleElasticity;
            public float blend;
            public float airDrag;
            public float lengthElasticity;
            public float elasticitySoften;
            public float radiusMultiplier;
            public AnimationCurve radiusCurve;
            
            public static JJiggleData FromJson(JObject jiggleData)
            {
                return new JJiggleData()
                {
                    gravityMultiplier = jiggleData["gravityMultiplier"].ToObject<float>(),
                    friction = jiggleData["friction"].ToObject<float>(),
                    angleElasticity = jiggleData["angleElasticity"].ToObject<float>(),
                    blend = jiggleData["blend"].ToObject<float>(),
                    airDrag = jiggleData["airDrag"].ToObject<float>(),
                    lengthElasticity = jiggleData["lengthElasticity"].ToObject<float>(),
                    elasticitySoften = jiggleData["elasticitySoften"].ToObject<float>(),
                    radiusMultiplier = jiggleData["radiusMultiplier"].ToObject<float>(),
                    radiusCurve = JPhysBone.GetAnimCurveFromJson(jiggleData["radiusCurve"].ToObject<JArray>())
                };
            }
        }

        public static JJiggleBone FromJson(JObject jiggleRig)
        {
            var windSplitStr = jiggleRig["wind"]?.ToObject<string>()?.Split(' ');
            Vector3 wind = new Vector3();
            if (windSplitStr != null && windSplitStr.Length >= 3)
            {
                wind = new Vector3(
                    x: float.Parse(windSplitStr[0]),
                    y: float.Parse(windSplitStr[1]),
                    z: float.Parse(windSplitStr[2]));
            }

            return new JJiggleBone()
            {
                wind = wind,
                colliders = jiggleRig["colliders"]?.ToObject<Il2CppSystem.Collections.Generic.List<string>>().ToArray(),
                name = jiggleRig["name"]?.ToObject<string>(),
                rootName = jiggleRig["rootName"]?.ToObject<string>(),
                data = JJiggleData.FromJson(jiggleRig["data"].Cast<JObject>())
            };
        }
    }



    public struct JPhysBone
    {
        public string name;
        public float damping;
        public AnimationCurve dampingCurve;
        public float elasticity;
        public AnimationCurve elasticityCurve;

        public float stiffness;
        public AnimationCurve stiffnessCurve;
        public float inert;
        public AnimationCurve inertCurve;
        public Vector3 gravity;

        public static JPhysBone FromJson(JObject json)
        {
            var gravitySplitStr = json["gravity"]?.ToObject<string>()?.Split(' ');
            Vector3 gravity = new Vector3();
            if (gravitySplitStr != null && gravitySplitStr.Length >= 3)
            {
                gravity = new Vector3(
                    x: float.Parse(gravitySplitStr[0]),
                    y: float.Parse(gravitySplitStr[1]),
                    z: float.Parse(gravitySplitStr[2]));
            }

            return new JPhysBone()
            {
                name = json["name"].ToObject<string>(),
                damping = json["damping"].ToObject<float>(),
                dampingCurve = GetAnimCurveFromJson(json["dampingCurve"].ToObject<JArray>()),
                elasticity = json["elasticity"].ToObject<float>(),
                elasticityCurve = GetAnimCurveFromJson(json["elasticityCurve"].ToObject<JArray>()),
                stiffness = json["stiffness"].ToObject<float>(),
                stiffnessCurve = GetAnimCurveFromJson(json["stiffnessCurve"].ToObject<JArray>()),
                inert = json["inert"].ToObject<float>(),
                inertCurve = GetAnimCurveFromJson(json["inertCurve"].ToObject<JArray>()),
               gravity = gravity
            };
        }

        public static AnimationCurve GetAnimCurveFromJson(JArray json)
        {
            List<Keyframe> keyframes = new List<Keyframe>();
            foreach (var jCurveKey in json._values)
            {
                var jCurveArrKey = jCurveKey.Cast<JArray>();
                int time = jCurveArrKey[0].ToObject<int>();
                int value = jCurveArrKey[1].ToObject<int>();
                keyframes.Add(new Keyframe(time, value));
            }
            return new AnimationCurve(keyframes.ToArray());
        }
    }


    public static void CopyRigsValues(GameObject newPrefab, Il2CppArrayBase<JiggleRigBuilder> oldJiggleRigs, Il2CppArrayBase<JiggleRigBuilder> newJiggleRigs)
    {
        for (int i = 0; i < oldJiggleRigs.Length; i++)
        {
            var oldBone = oldJiggleRigs[i];
            var newBone = newJiggleRigs[i];
            CopyRigValues(newPrefab, oldBone,newBone);
            newBone.Initialize();
        }
    }

    public static void CopyRigValues(GameObject newPrefab, JiggleRigBuilder oldRig, JiggleRigBuilder newRig)
    {
        newRig.colliders = new List<Collider>();
        newRig.wind = oldRig.wind;
        newRig.data = oldRig.data;
        //newRig.rootTransform = oldRig.rootTransform;
        var newTransformMap = new Dictionary<string, Transform>();// newPrefab.GetComponentsInChildren<Transform>().ToDictionary(tf => tf.name, tf => tf);
        foreach (var tfTransform in newPrefab.GetComponentsInChildren<Transform>())
        {
            newTransformMap.Add(tfTransform.name, tfTransform);
        }
        
        newRig.rootTransform = newTransformMap[oldRig.rootTransform.name];
        foreach (var collider in oldRig.colliders)
        {
            var newColliderTf = newTransformMap[collider.name];
            newRig.colliders.Add(newColliderTf.GetComponent<Collider>());
        }




        // if (newRig.jiggleGCHandle?.Target is JiggleSettings js)
        // {
        //     newRig.jiggleSettings = js;
        //     CustomCharacterLoaderPlugin.InjectComponent.Instance.Log.LogInfo($"Get GCHandle Target: {newRig.jiggleSettings}");
        // }
    }
}