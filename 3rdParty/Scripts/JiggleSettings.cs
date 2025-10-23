using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JigglePhysics {
    
#if UNITY_EDITOR
[CustomEditor(typeof(JiggleSettings))]
public class JiggleSettingsEditor : Editor {
    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        ((JiggleSettings)target).OnInspectorGUI(serializedObject);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif

public class JiggleSettings : JiggleSettingsBase {
    public float gravityMultiplier = 1f;
    public float friction = 0.4f;
    public float angleElasticity = 0.4f;
    public float blend = 1f;
    public float airDrag = 0.1f;
    public float lengthElasticity = 0.8f;
    public float elasticitySoften = 0f;
    public float radiusMultiplier = 0f;
    public AnimationCurve radiusCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));

    public override JiggleSettingsData GetData() {
        return new JiggleSettingsData {
            gravityMultiplier = gravityMultiplier,
            friction = friction,
            airDrag = airDrag,
            blend = blend,
            angleElasticity = angleElasticity,
            elasticitySoften = elasticitySoften,
            lengthElasticity = lengthElasticity,
            radiusMultiplier = radiusMultiplier,
            radiusCurve = radiusCurve
        };
    }
    public void SetData(JiggleSettingsData data) {
        gravityMultiplier = data.gravityMultiplier;
        friction = data.friction;
        angleElasticity = data.angleElasticity;
        blend = data.blend;
        airDrag = data.airDrag;
        lengthElasticity = data.lengthElasticity;
        elasticitySoften = data.elasticitySoften;
        radiusMultiplier = data.radiusMultiplier;
    }
    public override float GetRadius(float normalizedIndex) {
        return radiusMultiplier * radiusCurve.Evaluate(normalizedIndex);
    }
    public void SetRadiusCurve(AnimationCurve curve) {
        radiusCurve = curve;
    }

    #if UNITY_EDITOR
    private static bool advancedFoldout;
    private static bool collisionFoldout;
    public virtual void OnInspectorGUI(SerializedObject serializedObject) {
        advancedFoldout = EditorGUILayout.Foldout(advancedFoldout, new GUIContent("Advanced Settings", "Settings that are a little complicated and are only used to get a particular effect"));
        if (advancedFoldout) {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(airDrag)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(lengthElasticity)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(elasticitySoften)));
        }
        collisionFoldout = EditorGUILayout.Foldout(collisionFoldout, new GUIContent("Collision Settings", "Settings that are only used for collisions."));
        if (collisionFoldout) {
            EditorGUILayout.HelpBox( "Radius represents how close bones need to be to Colliders before depenetration occurs. If an individual element's radius is equal to, or less than 0, then collisions are disabled.", MessageType.Info);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(radiusMultiplier)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(radiusCurve)));
        }
    }

    [Serializable]
    private struct KeyframeData {
        public KeyframeData(Keyframe frame) {
            time = frame.time;
            value = frame.value;
            inTangent = frame.inTangent;
            outTangent = frame.outTangent;
            weightedMode = (int)frame.weightedMode;
            inWeight = frame.inWeight;
            outWeight = frame.outWeight;
        }
        [SerializeField] private float time;
        [SerializeField] private float value;
        [SerializeField] private float inTangent;
        [SerializeField] private float outTangent;
        [SerializeField] private int weightedMode;
        [SerializeField] private float inWeight;
        [SerializeField] private float outWeight;
        public Keyframe ToKeyframe() {
            return new Keyframe {
                time = time,
                value = value,
                inTangent = inTangent,
                outTangent = outTangent,
                weightedMode = (WeightedMode)weightedMode,
                inWeight = inWeight,
                outWeight = outWeight
            };
        }
    }

    [Serializable]
    private struct AnimationCurveData {
        public AnimationCurveData(AnimationCurve target) {
            List<KeyframeData> keyframeDatas = new List<KeyframeData>();
            foreach (var key in target.keys) {
                keyframeDatas.Add(new KeyframeData(key));
            }
            this.keyframeDatas = keyframeDatas.ToArray();
            preWrapMode = target.preWrapMode;
            postWrapMode = target.postWrapMode;
        }
        [SerializeField] private KeyframeData[] keyframeDatas;
        [SerializeField] private WrapMode preWrapMode;
        [SerializeField] private WrapMode postWrapMode;
        public AnimationCurve ToCurve() {
            List<Keyframe> keyframes = new List<Keyframe>();
            foreach (var keyData in keyframeDatas) {
                keyframes.Add(keyData.ToKeyframe());
            }
            return new AnimationCurve() {
                keys = keyframes.ToArray(),
                preWrapMode = preWrapMode,
                postWrapMode = postWrapMode,
            };
        }
    }

    [Serializable]
    private struct SettingsData {
        public SettingsData(JiggleSettings settings) {
            gravityMultiplier = settings.gravityMultiplier;
            friction = settings.friction;
            angleElasticity = settings.angleElasticity;
            blend = settings.blend;
            airDrag = settings.airDrag;
            lengthElasticity = settings.lengthElasticity;
            elasticitySoften = settings.elasticitySoften;
            radiusMultiplier = settings.radiusMultiplier;
            radiusCurve = new AnimationCurveData(settings.radiusCurve);
        }

        [SerializeField] private float gravityMultiplier;
        [SerializeField] private float friction;
        [SerializeField] private float angleElasticity;
        [SerializeField] private float blend;
        [SerializeField] private float airDrag;
        [SerializeField] private float lengthElasticity;
        [SerializeField] private float elasticitySoften;
        [SerializeField] private float radiusMultiplier;
        [SerializeField] private AnimationCurveData radiusCurve;

        public void ApplyTo(JiggleSettings target) {
            target.gravityMultiplier = gravityMultiplier;
            target.friction = friction;
            target.angleElasticity = angleElasticity;
            target.blend = blend;
            target.airDrag = airDrag;
            target.lengthElasticity = lengthElasticity;
            target.elasticitySoften = elasticitySoften;
            target.radiusMultiplier = radiusMultiplier;
            target.radiusCurve = radiusCurve.ToCurve();
        }
    }

    [ContextMenu("Copy Parameters")]
    private void CopyParameters() {
        string json = JsonUtility.ToJson(new SettingsData(this));
        GUIUtility.systemCopyBuffer = json;
    }
    [ContextMenu("Paste Parameters")]
    private void PasteParameters() {
        var data = JsonUtility.FromJson<SettingsData>(GUIUtility.systemCopyBuffer);
        Undo.RecordObject(this, "Pasted parameters");
        data.ApplyTo(this);
    }
    #endif
}

}