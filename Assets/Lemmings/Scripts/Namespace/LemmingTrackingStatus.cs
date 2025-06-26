
using UnityEngine;
using System.Linq;

// Safe inclusion for AR Foundation
#if UNITY_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

// Safe inclusion for Unity Input System + XR Toolkit
#if ENABLE_INPUT_SYSTEM && UNITY_XR_MANAGEMENT
using UnityEngine.InputSystem.XR;
#endif

// Safe inclusion for SteamVR — define STEAMVR_INPUT manually in Player Settings
#if STEAMVR_INPUT
using Valve.VR;
#endif

// Safe inclusion for Meta (OVR) SDK — define META_SDK_INSTALLED manually in Player Settings
#if META_SDK_INSTALLED
using OculusSampleFramework;
using OVRSkeleton = global::OVRSkeleton;
#endif

// Safe inclusion for XR Hands — UNITY_XR_HANDS is auto-defined when package is installed
#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace Lemmings.Utilities
{
    //TODO: This is a utility that developers will likely need to customize or replace depending on their project
    //TODO: WARNING - This is a stub and not ready for deployment
    
    /// <summary>
    /// IMPORTANT: This has not been tested for every SDK and you can expect errors with many SDKs.
    /// The goal is to provide a scaffold onto which you can generate project-specific solutions.
    /// SDKs update and evolve frequently and in order to prioritize felxibility, we did not want to
    /// create narrow dependencies. 
    /// 
    /// Static utility for detecting presence and tracking status of known tracking SDKs.
    /// Supports fallback to confidence threshold when no SDK is active.
    /// Provides modular methods for retrieving targets, identifying SDKs, and checking joint tracking status.
    /// </summary>
    public static class LemmingTrackingStatus
    {
        public enum TrackingSDK
        {
            Unknown,
            ARFoundation,
            XRToolkit,
            SteamVR,
            Meta,
            XRHands,
            Ultraleap,
            ManoMotion,
            VisionOS
        }

        /// <summary>
        /// Type-agnostic reference for identifying a tracked body part or joint.
        /// Used to unify SDK-specific identifiers.
        /// </summary>
        public struct LemmingTrackingReference
        {
            public TrackingSDK SDK;

#if UNITY_XR_HANDS
            public XRHandJointID? XRJointId;
#endif

#if META_SDK_INSTALLED
            public OVRSkeleton.BoneId? MetaBoneId;
#endif

            public enum Handedness { Unknown, Left, Right }
            public Handedness Hand;

            public string CustomLabel;
        }

        /// <summary>
        /// Holds all relevant tracking information for a given Lemming.
        /// </summary>
        public struct LemmingTrackingInfo
        {
            public Transform Source;
            public GameObject OverrideObject;
            public float Confidence;
            public Transform Target;
            public TrackingSDK SDK;
            public bool IsTracked;
            public LemmingTrackingReference Reference;
        }

        /// <summary>
        /// Attempts to resolve the specific joint Transform based on SDK and reference information.
        /// </summary>
        /// <param name="root">The root GameObject to search from.</param>
        /// <param name="reference">The reference info identifying the desired joint.</param>
        /// <param name="jointTransform">Outputs the resolved joint transform, if found.</param>
        /// <returns>True if the joint Transform was successfully located.</returns>
        public static bool TryResolveJointTransform(Transform root, in LemmingTrackingReference reference, out Transform jointTransform)
        {
            jointTransform = null;
            if (root == null) return false;

            switch (reference.SDK)
            {
#if META_SDK_INSTALLED
                case TrackingSDK.Meta:
                    var skeleton = root.GetComponentInChildren<OVRSkeleton>();
                    if (skeleton != null && skeleton.IsDataValid && reference.MetaBoneId.HasValue)
                    {
                        var joint = skeleton.Bones.FirstOrDefault(b => b.Id == reference.MetaBoneId.Value);
                        if (joint != null && joint.Transform != null)
                        {
                            jointTransform = joint.Transform;
                            return true;
                        }
                    }
                    break;
#endif
#if UNITY_XR_HANDS
                case TrackingSDK.XRHands:
                    var joints = root.GetComponentsInChildren<XRHandJoint>();
                    foreach (var joint in joints)
                    {
                        if (joint.id == reference.XRJointId && joint.tracked)
                        {
                            jointTransform = joint.transform;
                            return true;
                        }
                    }
                    break;
#endif
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Evaluates tracking for a Lemming and returns a boolean status.
        /// </summary>
        /// <param name="lemming">The Lemming to evaluate tracking for.</param>
        /// <param name="status">Returns true if tracking is currently valid.</param>
        /// <returns>True if the tracking evaluation completed successfully.</returns>
        public static bool GetTrackingStatus(Lemming lemming, out bool status)
        {
            var info = new LemmingTrackingInfo
            {
                Source = lemming.transform,
                OverrideObject = lemming.matchTarget,
                Confidence = lemming.Confidence
            };

            UpdateTrackingInfo(ref info);
            status = info.IsTracked;
            return true;
        }

        /// <summary>
        /// Populates a LemmingTrackingInfo struct with the best available target, active SDK, and tracking result.
        /// </summary>
        /// <param name="info">The tracking info container to be updated.</param>
        public static void UpdateTrackingInfo(ref LemmingTrackingInfo info)
        {
            info.Target = info.OverrideObject != null ? info.OverrideObject.transform : info.Source;

            if (GetSDK(info.Target, out var sdk))
            {
                info.SDK = sdk;
                GetStatus(sdk, info.Target, info.Confidence, out var tracked);
                info.IsTracked = tracked;
                info.Reference.SDK = sdk;
            }
            else
            {
                info.SDK = TrackingSDK.Unknown;
                info.IsTracked = IsConfidenceTracked(info.Confidence);
                info.Reference.SDK = TrackingSDK.Unknown;
            }
        }

                /// <summary>
        /// Attempts to automatically populate a LemmingTrackingReference from the given root object.
        /// This is a heuristic and works best if a known joint/bone structure exists under the target.
        /// </summary>
        /// <param name="root">The root object to scan for tracking components.</param>
        /// <param name="reference">Outputs a populated tracking reference if possible.</param>
        /// <returns>True if a reference could be populated based on discovered SDK and structure.</returns>
        public static bool TryAutoPopulateReference(Transform root, out LemmingTrackingReference reference)
        {
            reference = new LemmingTrackingReference
            {
                SDK = TrackingSDK.Unknown,
                Hand = LemmingTrackingReference.Handedness.Unknown,
                CustomLabel = ""
            };

            if (root == null) return false;

#if META_SDK_INSTALLED
            var skeleton = root.GetComponentInChildren<OVRSkeleton>();
            if (skeleton != null && skeleton.IsDataValid)
            {
                var bone = skeleton.Bones.FirstOrDefault(b => b.Id == OVRSkeleton.BoneId.Hand_IndexTip);
                if (bone != null)
                {
                    reference.SDK = TrackingSDK.Meta;
                    reference.MetaBoneId = OVRSkeleton.BoneId.Hand_IndexTip;
                    reference.Hand = skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.HandLeft
                        ? LemmingTrackingReference.Handedness.Left
                        : LemmingTrackingReference.Handedness.Right;
                    reference.CustomLabel = bone.Transform?.name;
                    return true;
                }
            }
#endif

#if UNITY_XR_HANDS
            var joints = root.GetComponentsInChildren<XRHandJoint>();
            var joint = joints.FirstOrDefault(j => j.id == XRHandJointID.IndexTip);
            if (joint != null && joint.tracked)
            {
                reference.SDK = TrackingSDK.XRHands;
                reference.XRJointId = XRHandJointID.IndexTip;
                reference.Hand = joint.handedness == UnityEngine.XR.Hands.Handedness.Left
                    ? LemmingTrackingReference.Handedness.Left
                    : LemmingTrackingReference.Handedness.Right;
                reference.CustomLabel = joint.name;
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Attempts to identify the tracking SDK in use on the target object.
        /// </summary>
        /// <param name="target">The GameObject's transform to evaluate.</param>
        /// <param name="sdk">Outputs the SDK that appears to be in use.</param>
        /// <returns>True if a supported SDK was identified.</returns>
        public static bool GetSDK(Transform target, out TrackingSDK sdk)
        {
            sdk = TrackingSDK.Unknown;
            if (target == null) return false;

#if UNITY_AR_FOUNDATION
            if (target.GetComponent<ARTrackable>() != null) { sdk = TrackingSDK.ARFoundation; return true; }
#endif
#if ENABLE_INPUT_SYSTEM && UNITY_XR_MANAGEMENT
            sdk = TrackingSDK.XRToolkit; return true;
#endif
#if STEAMVR_INPUT
            if (target.GetComponent<SteamVR_Behaviour_Pose>() != null) { sdk = TrackingSDK.SteamVR; return true; }
#endif
#if META_SDK_INSTALLED
            if (target.GetComponent<OVRHand>() != null || target.GetComponent<OVRSkeleton>() != null) { sdk = TrackingSDK.Meta; return true; }
#endif
#if UNITY_XR_HANDS
            if (target.GetComponent<XRHandJoint>() != null) { sdk = TrackingSDK.XRHands; return true; }
#endif
#if ULTRALEAP_TRACKING
            if (target.GetComponent<Leap.Unity.LeapProvider>() != null) { sdk = TrackingSDK.Ultraleap; return true; }
#endif
#if MANOMOTION_SDK
            sdk = TrackingSDK.ManoMotion; return true;
#endif
#if UNITY_VISIONOS
            sdk = TrackingSDK.VisionOS; return true;
#endif
            return false;
        }

        /// <summary>
        /// Evaluates whether tracking is currently active for a given SDK and target.
        /// </summary>
        /// <param name="sdk">The identified SDK.</param>
        /// <param name="target">The transform associated with the tracked object.</param>
        /// <param name="confidence">Fallback confidence value (e.g. from custom tracking systems).</param>
        /// <param name="status">Outputs whether the system is currently tracking.</param>
        /// <returns>True if evaluation was successful.</returns>
        public static bool GetStatus(TrackingSDK sdk, Transform target, float confidence, out bool status)
        {
            status = false;
            switch (sdk)
            {
                case TrackingSDK.ARFoundation: status = IsARFoundationTracked(target); return true;
                case TrackingSDK.XRToolkit:    status = IsXRTracked(target);           return true;
                case TrackingSDK.SteamVR:      status = IsSteamVRTracked(target);      return true;
                case TrackingSDK.Meta:         status = IsMetaTracked(target) || IsMetaJointTracked(target); return true;
                case TrackingSDK.XRHands:      status = IsXRJointTracked(target);      return true;
                case TrackingSDK.Ultraleap:    status = IsUltraLeapTracked(target);    return true;
                case TrackingSDK.ManoMotion:   status = IsManoMotionTracked(target);   return true;
                case TrackingSDK.VisionOS:     status = IsVisionOSTracked(target);     return true;
                case TrackingSDK.Unknown:      status = IsConfidenceTracked(confidence); return true;
            }
            return false;
        }

        /// <summary>
        /// Fallback confidence check for systems without SDKs.
        /// </summary>
        public static bool IsConfidenceTracked(float confidence, float threshold = 0.2f) => confidence >= threshold;

        /// <summary>
        /// Returns true if AR Foundation is tracking this object.
        /// </summary>
        public static bool IsARFoundationTracked(Transform target)
        {
#if UNITY_AR_FOUNDATION
            var trackable = target?.GetComponent<ARTrackable>();
            return trackable != null && trackable.trackingState == TrackingState.Tracking;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if XR Toolkit input system has valid tracking.
        /// </summary>
        public static bool IsXRTracked(Transform target)
        {
#if ENABLE_INPUT_SYSTEM && UNITY_XR_MANAGEMENT
            var device = XRController.leftHand ?? XRController.rightHand;
            return device != null && device.isTracked.isPressed;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if SteamVR reports this pose as valid.
        /// </summary>
        public static bool IsSteamVRTracked(Transform target)
        {
#if STEAMVR_INPUT
            var pose = target?.GetComponent<SteamVR_Behaviour_Pose>();
            return pose != null && pose.isValid;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if Meta hand tracking is enabled.
        /// </summary>
        public static bool IsMetaTracked(Transform target)
        {
#if META_SDK_INSTALLED
            var hand = target?.GetComponent<OVRHand>();
            return hand != null && hand.IsTracked;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if Meta skeleton joints are available and active.
        /// </summary>
        public static bool IsMetaJointTracked(Transform target)
        {
#if META_SDK_INSTALLED
            var skeleton = target?.GetComponent<OVRSkeleton>();
            if (skeleton != null && skeleton.IsDataValid)
            {
                var joints = skeleton.Bones;
                return joints.Any(j => j.Transform != null && j.Transform.gameObject.activeInHierarchy);
            }
#endif
            return false;
        }

        /// <summary>
        /// Returns true if XR Hands joint is valid and tracked.
        /// </summary>
        public static bool IsXRJointTracked(Transform target)
        {
#if UNITY_XR_HANDS
            var joint = target?.GetComponent<XRHandJoint>();
            return joint != null && joint.tracked;
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if Ultraleap provider detects tracked hands.
        /// </summary>
        public static bool IsUltraLeapTracked(Transform target)
        {
#if ULTRALEAP_TRACKING
            var provider = target?.GetComponent<Leap.Unity.LeapProvider>();
            var frame = provider?.CurrentFrame;
            return frame != null && frame.Hands.Any(h => h.IsTracked);
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns true if ManoMotion reports tracking.
        /// </summary>
        public static bool IsManoMotionTracked(Transform target)
        {
#if MANOMOTION_SDK
            return ManoMotionManager.Instance.Hand_infos.hand_info.tracking_info.is_tracked;
#else
            return false;
#endif
        }

        /// <summary>
        /// Placeholder: Returns true if VisionOS is assumed active.
        /// </summary>
        public static bool IsVisionOSTracked(Transform target)
        {
#if UNITY_VISIONOS
            return true;
#else
            return false;
#endif
        }
    }
}
