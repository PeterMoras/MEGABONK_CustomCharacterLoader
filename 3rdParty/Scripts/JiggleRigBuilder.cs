using CustomCharacterLoader;
using Il2CppSystem.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
namespace JigglePhysics {
    
//[DefaultExecutionOrder(200)]
public class JiggleRigBuilder : MonoBehaviour {
    public static float maxCatchupTime => Time.fixedDeltaTime*4;

    //[Tooltip("Enables interpolation for the simulation, this should be enabled unless you *really* need the simulation to only update on FixedUpdate.")]
    //public bool interpolate = true;
    public Transform rootTransform;
    public List<Transform> ignoredTransforms = new List<Transform>();
    public List<Collider> colliders = new List<Collider>();

    public Vector3 wind;
    public JiggleRigLOD levelOfDetail;
    private bool debugDraw;
    public JiggleSettingsData data;

    
    private bool initialized;

    public Transform GetRootTransform() => rootTransform;

    private bool NeedsCollisions => colliders.Count != 0;

    protected List<JiggleBone> simulatedPoints;

    

    public void MatchAnimationInstantly() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.MatchAnimationInstantly();
        }
    }

    public void UpdateJiggle(Vector3 wind, double time) {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.VerletPass(data, wind, time);
        }

        if (NeedsCollisions) {
            for (int i = simulatedPoints.Count - 1; i >= 0; i--) {
                simulatedPoints[i].CollisionPreparePass(data);
            }
        }
        
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.ConstraintPass(data);
        }
        
        if (NeedsCollisions) {
            foreach (JiggleBone simulatedPoint in simulatedPoints) {
                simulatedPoint.CollisionPass(data, colliders);
            }
        }
        
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.SignalWritePosition(time);
        }
    }


    public void DeriveFinalSolve() {
        Vector3 virtualPosition = simulatedPoints[0].DeriveFinalSolvePosition(Vector3.zero);
        Vector3 offset = simulatedPoints[0].transform.position - virtualPosition;
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.DeriveFinalSolvePosition(offset);
        }
    }

    public void Pose(bool debugDraw) {
        DeriveFinalSolve();
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PoseBone(data.blend);
            if (debugDraw) {
                simulatedPoint.DebugDraw(Color.red, Color.blue, true);
            }
        }
    }

    public void PrepareTeleport() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PrepareTeleport();
        }
    }

    public void FinishTeleport() {
        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.FinishTeleport();
        }
    }

    private void LateUpdate()
    {
        CachedSphereCollider.StartPass();
        Advance(Time.deltaTime);
        CachedSphereCollider.FinishedPass();

    }


    private double accumulation;
    private bool dirtyFromEnable = false;
    private bool wasLODActive = true;

    private void Awake() {
        Initialize();
    }
    void OnEnable() {
        JiggleRigHandler.AddBuilder(this);
        dirtyFromEnable = true;
    }
    void OnDisable() {
        JiggleRigHandler.RemoveBuilder(this);
        PrepareTeleport();
    }

    public void Initialize() {
        accumulation = 0f;
        //jiggleRigs ??= new List<JiggleRig>();
        simulatedPoints = new List<JiggleBone>();
        if (rootTransform == null) {
            return;
        }

        RigHelper.CreateSimulatedPoints(simulatedPoints, ignoredTransforms, rootTransform, null);
        foreach (var simulatedPoint in simulatedPoints) {
            simulatedPoint.CalculateNormalizedIndex();
        }
        initialized = true;
    }

    public virtual void Advance(float deltaTime) {
        if (levelOfDetail!=null && !levelOfDetail.CheckActive(transform.position)) {
            if (wasLODActive) PrepareTeleport();
            wasLODActive = false;
            return;
        }
        if (!wasLODActive) FinishTeleport();
        RigHelper.PrepareBone(transform.position, levelOfDetail, out var newData, initialized, data, simulatedPoints);
        this.data = newData;
        
        if (dirtyFromEnable) {
            FinishTeleport();
            dirtyFromEnable = false;
        }

        accumulation = Math.Min(accumulation+deltaTime, maxCatchupTime);
        while (accumulation > Time.fixedDeltaTime) {
            accumulation -= Time.fixedDeltaTime;
            double time = Time.timeAsDouble - accumulation;
            UpdateJiggle(wind, time);
        }
        
        Pose(debugDraw);
        wasLODActive = true;
    }

    //private void LateUpdate() {
        //if (!interpolate) {
            //return;
        //}
        //Advance(Time.deltaTime);
    //}

    private void OnDrawGizmos() {
        if (!initialized || simulatedPoints == null) {
            Initialize();
        }
        // foreach (JiggleBone simulatedPoint in simulatedPoints) {
        //     simulatedPoint.OnDrawGizmos(data);
        // }
    }

    private void OnValidate() {
        if (rootTransform == null) {
            rootTransform = transform;
        }
        if (Application.isPlaying) return;
        Initialize();
    }
}

public static class RigHelper
{
    public static void PrepareBone(Vector3 position, JiggleRigLOD jiggleRigLOD, out JiggleSettingsData data, bool initialized, JiggleSettingsData jiggleSettings, List<JiggleBone> simulatedPoints) {
        if (!initialized) {
            Debug.LogError("JiggleRig was never initialized. Please call JiggleRig.Initialize() if you're going to manually timestep.");
        }

        foreach (JiggleBone simulatedPoint in simulatedPoints) {
            simulatedPoint.PrepareBone();
        }

        data = jiggleSettings;
        data = jiggleRigLOD!=null ? jiggleRigLOD.AdjustJiggleSettingsData(position, data):data;
    }
    public static void CreateSimulatedPoints(List<JiggleBone> outputPoints, 
        List<Transform> ignoredTransforms, Transform currentTransform, JiggleBone parentJiggleBone) {
        //CustomCharacterLoaderPlugin.InjectComponent.Instance.Log.LogInfo("Start Create Simulated Points");
        JiggleBone newJiggleBone = new JiggleBone(currentTransform, parentJiggleBone);
        outputPoints.Add(newJiggleBone);
        //CustomCharacterLoaderPlugin.InjectComponent.Instance.Log.LogInfo("added output point");

        // Create an extra purely virtual point if we have no children.
        if (currentTransform.childCount == 0) {
            if (newJiggleBone.parent == null) {
                if (newJiggleBone.transform.parent == null) {
                    //CustomCharacterLoaderPlugin.InjectComponent.Instance.Log.LogInfo("Can't have a singular jiggle bone with no parents. That doesn't even make sense!");
                } else {
                    outputPoints.Add(new JiggleBone(null, newJiggleBone));
                    return;
                }
            }
            outputPoints.Add(new JiggleBone(null, newJiggleBone));
            return;
        }
        //CustomCharacterLoaderPlugin.InjectComponent.Instance.Log.LogInfo("for loop");
        for (int i = 0; i < currentTransform.childCount; i++) {
            if (ignoredTransforms.Contains(currentTransform.GetChild(i))) {
                continue;
            }
            CreateSimulatedPoints(outputPoints, ignoredTransforms, currentTransform.GetChild(i), newJiggleBone);
        }
    }
}
}