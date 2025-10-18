/*
 * PhysicsBone 1.3.0
 * Script written by Abcight
 * https://github.com/Abcight/PhysicsBone
 * 
 *  MIT License
    Copyright (c) 2020 Abcight

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using UnityEngine;
using Random = UnityEngine.Random;

namespace Abcight
{
    public class PhysicsBone : MonoBehaviour
    {


       
        public float damping = 1;

        public float gravityScale = 1f;

        public AnimationCurve forceFaloff = new AnimationCurve(new Keyframe[] { new Keyframe(0, 0), new Keyframe(1, 1) });

        public bool doCollisionChecks = true;

        public float collisionAccuracy = 1000f;

        public float collisionRadius = 0.1f;

        public AnimationCurve collisionRadiusDistrib = new AnimationCurve(new Keyframe[] { new Keyframe(0, 1), new Keyframe(1, 1) });

        // This is still unimplemented, you can make it serializable but don't report bugs
        private CollisionMode collisionMode = CollisionMode.CenterPushout;
        private enum CollisionMode { CenterPushout, Piled }

        public LayerMask collisionLayerMask = ~0;

        public bool reactToWindZones = false;

        public float windScale = 1;


        public Transform distanceCheckTarget;
        public float updateDistance = 15f;

        public Transform[] bones;
        public Vector3[] boneLocalPositions;
        public Quaternion[] boneLocalRotations;
        public float[] boneLocalDistances;

        public Vector3[] lastFrameBonePositions;
        public Quaternion gravityRootRotation;
        public float rootLocalEulerY;

        public WindZone[] windZones;

        public bool restored;

        // This is still not fully implemented
        public bool collisionInChildren;

        // Setup
        public void Start()
        {
            bones = GetComponentsInChildren<Transform>();

            boneLocalPositions = new Vector3[bones.Length];
            lastFrameBonePositions = new Vector3[bones.Length];
            boneLocalRotations = new Quaternion[bones.Length];
            boneLocalDistances = new float[bones.Length];
            //windZones = FindObjectsOfType<WindZone>();

            for (int i = 0; i < bones.Length; i++)
            {
                lastFrameBonePositions[i] = bones[i].position;
                boneLocalPositions[i] = bones[i].localPosition;
                boneLocalRotations[i] = bones[i].localRotation;
                if (i > 0) boneLocalDistances[i] = Vector3.Distance(bones[i].position, bones[i].parent.position);
            }
            rootLocalEulerY = bones[0].localEulerAngles.y;
            gravityRootRotation = bones[0].rotation;
        }

        public void LateUpdate()
        {
            if(assertDistanceUpdateCheck()) return;

            Vector3 deltaMove = lastFrameBonePositions[0] - bones[0].position;
            bones[0].Rotate(deltaMove * 100);

            //if (reactToWindZones) handleWindZones();

            for (int i = 0; i < bones.Length; i++)
            {
                bones[i].position = lastFrameBonePositions[i];
                bones[0].localPosition = boneLocalPositions[0];

                handleGravity();

                if (i > 0)
                {
                    handleMovement(i);
                    handleStretching(i);
                    if (doCollisionChecks) handleCollision(i);
                }
            }

            //set last frame positions
            for (int i = 0; i < bones.Length; i++)
            {
                lastFrameBonePositions[i] = bones[i].position;
            }
        }

        private bool assertDistanceUpdateCheck()
        {
            if (distanceCheckTarget != null && Vector3.Distance(transform.position, distanceCheckTarget.position) > updateDistance)
            {
                if (!restored)
                {
                    for (int i = 0; i < bones.Length; i++)
                    {
                        bones[i].localPosition = boneLocalPositions[i];
                        bones[i].localRotation = boneLocalRotations[i];
                    }
                    restored = true;
                }
                return true;
            }
            else
            {
                restored = false;
            }
            return false;
        }

        private void handleGravity()
        {
            float gravityForce = gravityScale / Mathf.Clamp(damping, 0, 4) * Time.deltaTime;

            Quaternion boneRotation = bones[0].rotation;
            bones[0].localEulerAngles = new Vector3(0, rootLocalEulerY, 0);
            float worldSpaceY = bones[0].eulerAngles.y;
            bones[0].rotation = boneRotation;
            Quaternion targetGravityRotation = Quaternion.Euler(new Vector3(gravityRootRotation.eulerAngles.x, worldSpaceY, gravityRootRotation.eulerAngles.z));

            bones[0].rotation = Quaternion.Lerp(bones[0].rotation, targetGravityRotation, gravityForce);
        }

        private void handleWindZones()
        {
            foreach (WindZone zone in windZones)
            {
                if (!zone.gameObject.activeInHierarchy) continue;
                float turbulence = Random.Range(0f, 0.9f) * zone.windTurbulence;
                float windSine = Mathf.Sin((Time.time * zone.windPulseFrequency * 100 + turbulence));
                if (zone.mode == WindZoneMode.Directional || (Vector3.Distance(bones[0].position, zone.transform.position) <= zone.radius))
                {
                    bones[0].Rotate((zone.transform.forward * Mathf.Abs(windSine) * zone.windMain * 200) * windScale * Time.deltaTime);
                }
            }
        }

        private void handleMovement(int i)
        {
            float percentage = ((float)i / bones.Length);
            float curveValue = (1 - forceFaloff.Evaluate(percentage));

            bones[i].localPosition = Vector3.Lerp(bones[i].localPosition, boneLocalPositions[i], curveValue * Time.deltaTime * 10 * (5 - damping));
            bones[i].localRotation = Quaternion.Lerp(bones[i].localRotation, boneLocalRotations[i], curveValue * Time.deltaTime * 10 * (5 - damping));
        }


        private void handleStretching(int i)
        {
            float distanceToParent = Vector3.Distance(bones[i].parent.position, bones[i].position);
            if (distanceToParent != boneLocalDistances[i])
            {
                float difference = distanceToParent - boneLocalDistances[i];
                Vector3 direction = (bones[i].parent.position - bones[i].position).normalized;
                bones[i].position = bones[i].position + (direction * difference);
            }
        }

        private void handleCollision(int i)
        {
            collisionInChildren = false;

            float percentage = (float)i / bones.Length;
            float distrib = collisionRadiusDistrib.Evaluate(percentage);
            // Prevent from crashing
            collisionAccuracy = Mathf.Clamp(collisionAccuracy, 1, float.MaxValue);

            Collider[] collisions = Physics.OverlapSphere(bones[i].position, collisionRadius * distrib, collisionLayerMask);
            foreach (Collider collider in collisions)
            {
                if (collider is MeshCollider) continue;
                collisionInChildren = true;
                Vector3 point = collider.ClosestPoint(bones[i].position);
                Vector3 outDirection = Vector3.zero;
                switch (collisionMode)
                {
                    // Pushes out of the collider from it's center
                    case CollisionMode.CenterPushout:
                        outDirection = (bones[i].position - collider.bounds.center).normalized;
                        while (Vector3.Distance(point, bones[i].position) < collisionRadius)
                        {
                            bones[i].position = bones[i].position + (outDirection / collisionAccuracy);
                        }
                        break;
                    // UNFINISHED
                    // Pushes out towards the direction of closest surface point
                    case CollisionMode.Piled:
                        outDirection = (bones[i].position - collider.ClosestPointOnBounds(bones[i].position)).normalized;
                        float targetDistance = Vector3.Distance(bones[i].position, collider.ClosestPointOnBounds(bones[i].position));
                        if (Vector3.Distance(point, bones[i].position) < collisionRadius)
                        {
                            Debug.DrawLine(bones[i].position, collider.ClosestPointOnBounds(bones[i].position), Color.red);
                            bones[i].position = bones[i].position + (outDirection * targetDistance);
                        }
                        break;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            collisionAccuracy = Mathf.Clamp(collisionAccuracy, 1, float.MaxValue);

            Transform[] bn = GetComponentsInChildren<Transform>();
            Gizmos.color = Color.yellow;
            for (int i = 0; i < bn.Length; i++)
            {
                float percentage = (float)i / bn.Length;
                float distrib = collisionRadiusDistrib.Evaluate(percentage);
                Gizmos.DrawWireSphere(bn[i].position, collisionRadius * distrib);
            }
        }
#endif
    }

}