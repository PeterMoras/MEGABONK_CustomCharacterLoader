using Newtonsoft.Json.Linq;
using UnityEngine;

namespace CustomCharacterLoader;

public class PhysBoneAdder
{
    public static void SetBonesOnPrefab(GameObject prefab, JArray boneJson)
    {
        var log = CustomCharacterLoaderPlugin.InjectComponent.Instance.Log;
        log.LogDebug($"Loading {boneJson._values.Count} Phys Bones");

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

        static AnimationCurve GetAnimCurveFromJson(JArray json)
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
}