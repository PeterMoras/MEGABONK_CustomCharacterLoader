using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MyPhysicsBone : MonoBehaviour
{
    public float damping = 0.4f;
    public AnimationCurve dampingCurve= new AnimationCurve(new Keyframe(0f,0.8f),new Keyframe(1f,1f));
    public float stiffness = 1f;
    public AnimationCurve stiffnessCurve= new AnimationCurve(new Keyframe(0f,1f),new Keyframe(1f,1f));

    public float elasticity = 0.15f;
    public AnimationCurve elasticityCurve = new AnimationCurve(new Keyframe(0f,1f),new Keyframe(1f,0.8f));
    
    public float inert = 0.15f;
    public AnimationCurve inertCurve = new AnimationCurve(new Keyframe(0f,1),new Keyframe(0.5f,0.2f),new Keyframe(1f,1f));

    // public float maxDistanceReset = 0.1f;
    public Vector3 gravity = Vector3.zero;


    private Vector3[] particlePositions; // Positions of each segment as of last frame
    private Vector3[] previousBonePositions;
    private Vector3[] previousParticlePositions;

    private Transform[] bones = new Transform[0];
    private float[] segmentLengths;
    
    // Start is called before the first frame update
    void Start()
    {
        InitializeBones();
    }

    void LateUpdate()
    {
        SimulateBonePhysics();
    }


    // Initialize rope positions
    void InitializeBones()
    {
        
        var children = this.GetComponentsInChildren<Transform>();
        bones = new[] { this.transform.parent }.Concat(children).ToArray();
        
        var segmentCount = bones.Length;
        segmentLengths = new float[segmentCount-1];
        particlePositions = new Vector3[segmentCount];
        previousBonePositions = new Vector3[segmentCount];
        previousParticlePositions = new Vector3[segmentCount];
        // Initialize positions 
        for (int i = 0; i < segmentCount; i++)
        {
            particlePositions[i] = bones[i].position;
            previousBonePositions[i] = bones[i].position;
            previousParticlePositions[i] = particlePositions[i];
            if (i > 0)
            {
                segmentLengths[i-1] = (particlePositions[i] - particlePositions[i-1]).magnitude;
            }
        }
    }
    
    // Physics simulation loop
    void SimulateBonePhysics()
    {
        var dt = 1;//* Time.deltaTime;
        
        for (int iter = 0; iter < 1; iter++)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var boneTime = i / (bones.Length-1);
                var c_damping = damping * dampingCurve.Evaluate(boneTime);
                var c_elasticity = elasticity * elasticityCurve.Evaluate(boneTime);
                var c_stiffness = stiffness * stiffnessCurve.Evaluate(boneTime);
                var c_inert = inert * inertCurve.Evaluate(boneTime);
                Vector3 targetPosition = bones[i].position;
                Vector3 particlePosition = particlePositions[i];
                
                Vector3 particleVelocity = particlePosition - previousParticlePositions[i];
                //the movement the bones would normally perform over this timestep, assuming no active particles
                Vector3 boneVelocity = bones[i].position - previousBonePositions[i];
                // if ((targetPosition - particlePosition).magnitude > maxDistanceReset)
                // {
                //     particlePosition = targetPosition;
                //     particleVelocity = new Vector3();
                // }
                //all movement that is being forced upon the bones
                Vector3 externalMovement = gravity * (dt * dt) + (particleVelocity * (dt * (1 - c_damping))) + (boneVelocity * (dt* (1-c_stiffness)));
                
                Vector3 errorVector = (targetPosition - particlePosition);
                //float error = errorVector.magnitude;
                Vector3 correction = errorVector * (c_elasticity);
                
                previousParticlePositions[i] = particlePositions[i];
                particlePositions[i] = particlePosition + correction + externalMovement;
                particlePositions[i] = Vector3.Lerp(particlePositions[i], targetPosition, c_inert);
            
            }
        }

        particlePositions[0] = bones[0].position;

        for (int i = 0; i < particlePositions.Length; i++)
        {
            previousBonePositions[i] = bones[i].position;
            bones[i].position = particlePositions[i];
        }
        
        

        
    }
    
    private void OnDrawGizmos()
    {
        if (bones.Length > 0)
        {
            // Visualize the bone chain connections in the scene view
            Gizmos.color = Color.cyan;
            for (int i = 0; i < bones.Length - 1; i++)
            {
                Gizmos.DrawLine(bones[i].position, bones[i + 1].position);
            }
            Gizmos.color = Color.blue;
            for (int i = 0; i < particlePositions.Length - 1; i++)
            {
                Gizmos.DrawLine(previousParticlePositions[i], previousParticlePositions[i + 1]);
            }
        }
    }

    public static void CopyValuesTo(MyPhysicsBone oldBone, MyPhysicsBone newBone)
    {
        newBone.stiffness = oldBone.stiffness;
        newBone.damping = oldBone.damping;
        newBone.elasticity = oldBone.elasticity;
        newBone.gravity = oldBone.gravity;
        newBone.inert = oldBone.inert;
        
        newBone.dampingCurve = oldBone.dampingCurve;
        newBone.elasticityCurve = oldBone.elasticityCurve;
        newBone.inertCurve = oldBone.inertCurve;
        newBone.stiffnessCurve = oldBone.stiffnessCurve;
    }
}
