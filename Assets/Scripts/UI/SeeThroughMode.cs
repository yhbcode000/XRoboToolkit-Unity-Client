using Unity.XR.PXR;
using UnityEngine;

/// <summary>
/// Owns the video-see-through (passthrough) state, so it is decided in one place instead of three.
///
/// Operator requirement (2026-10-01): **immersive by default, passthrough only while recording.** The app
/// previously enabled VST unconditionally - in <c>Awake</c>, in <c>OnEnable</c>, and again on every resume -
/// which means the headset spent its whole session in passthrough. That is both unwanted and a plausible
/// reason a streamed video panel never became visible: in VST the compositor is rendering the camera feed,
/// so an additive UI panel can be hidden behind or composited away with it.
///
/// Recording is the one case that wants passthrough, because the recorded vision data *is* the camera
/// image. <see cref="UICameraCtrl"/> brackets its record start/stop around
/// <see cref="SetRecording"/>, and every other caller asks for the state rather than forcing it true.
/// </summary>
public static class SeeThroughMode
{
    /// <summary>False unless a recording is in progress. Starts false so a cold launch is immersive.</summary>
    public static bool Recording { get; private set; }

    /// <summary>Apply the see-through state. Immersive means both the manager flag and the VST camera off.</summary>
    public static void Apply(bool enable)
    {
        PXR_Manager.EnableVideoSeeThrough = enable;

        if (Application.platform != RuntimePlatform.Android)
        {
            return;
        }

        if (enable)
        {
            PXR_Enterprise.OpenVSTCamera();
        }
        else
        {
            PXR_Enterprise.CloseVSTCamera();
        }

        Debug.Log("[SeeThroughMode] see-through " + (enable ? "ON (recording)" : "OFF (immersive)"));
    }

    /// <summary>Bracket a recording. Called by UICameraCtrl around start/stop.</summary>
    public static void SetRecording(bool recording)
    {
        Recording = recording;
        Apply(recording);
    }

    /// <summary>
    /// What to apply on startup, on enable and on resume: immersive unless a recording is still running.
    /// Never forces passthrough on, which is what the old code did at all three sites.
    /// </summary>
    public static void ApplyCurrentOrDefault()
    {
        Apply(Recording);
    }
}
