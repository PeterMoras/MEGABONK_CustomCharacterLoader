using System;
using System.Collections;
using System.Collections.Generic;
using JigglePhysics;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JigglePhysics {

    public class JiggleRigSimpleLOD : JiggleRigLOD {

        float distance;
        float blend;
        private Camera currentCamera;

        private bool TryGetCamera(out Camera camera) {
            #if UNITY_EDITOR
            if (EditorWindow.focusedWindow is SceneView view) {
                camera = view.camera;
                return camera != null;
            }
            #endif
            if (currentCamera == null || !currentCamera.CompareTag("MainCamera")) {
                currentCamera = Camera.main;
            }
            camera = currentCamera;
            return currentCamera != null;
        }

        [NonSerialized] Transform cameraTransform;

        public override bool CheckActive(Vector3 position) {
            if (!TryGetCamera(out Camera camera)) {
                return false;
            }
            return Vector3.Distance(camera.transform.position, position) < distance;
        }

        public override JiggleSettingsData AdjustJiggleSettingsData(Vector3 position, JiggleSettingsData data) {
            if (!TryGetCamera(out Camera camera)) {
                return data;
            }
            
            var currentBlend = (Vector3.Distance(camera.transform.position, position) - distance + blend) / blend;
            currentBlend = Mathf.Clamp01(1f-currentBlend);
            data.blend = currentBlend;
            return data;
        }

    }

}