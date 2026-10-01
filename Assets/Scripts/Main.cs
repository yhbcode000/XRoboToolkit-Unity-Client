using Unity.XR.PICO.TOBSupport;
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

public class Main : MonoBehaviour
{
    private void Awake()
    {
        CrashProbe.Initialize();
        CrashProbe.Breadcrumb("main.awake");
        // Apply any operator overrides pushed by the pipeline before the UI builds, so the dialogs
        // open with the right address already in place and no keyboard is needed.
        RemoteVisionBootstrap.ApplyOperatorOverrides();
        DebugManager.instance.enableRuntimeUI = false;
        Application.logMessageReceived += OnLogMessageReceived;
        XRSettings.eyeTextureResolutionScale = 1.5f;
        // Immersive by default; passthrough only while a recording is running. This used to be a bare
        // `EnableVideoSeeThrough = true`, which put the headset in passthrough for the whole session.
        SeeThroughMode.ApplyCurrentOrDefault();
        //Closing the security fence is only effective on B-end devices.
        PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SECURITY_ZONE_PERMANENTLY, SwitchEnum.S_OFF);
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        LogView.Push(condition, stackTrace, type);
        if (type == LogType.Error || type == LogType.Exception)
        {
            Toast.Show(condition);
        }
    }

    private void Update()
    {
        CrashProbe.Tick();
    }

    private void OnEnable()
    {
        CrashProbe.Lifecycle("enable");
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("OnEnable");
            // Was an unconditional OpenVSTCamera(), i.e. passthrough on every enable.
            SeeThroughMode.ApplyCurrentOrDefault();
        }
    }

    private void OnDisable()
    {
        CrashProbe.Lifecycle("disable");
        if (Application.platform == RuntimePlatform.Android)
        {
            Debug.Log("OnDisable");
            PXR_Enterprise.CloseVSTCamera();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        CrashProbe.Lifecycle(pauseStatus ? "pause" : "resume");
        Debug.Log("OnApplicationPause " + pauseStatus);
        if (pauseStatus)
        {
            PXR_Enterprise.CloseVSTCamera();
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SECURITY_ZONE_PERMANENTLY,
                SwitchEnum.S_ON);
        }
        else
        {
            // Was `EnableVideoSeeThrough = true` plus an unconditional OpenVSTCamera() on every resume,
            // which is why taking the headset off and putting it back on always landed in passthrough.
            SeeThroughMode.ApplyCurrentOrDefault();
            //Closing the security fence is only effective on B-end devices.
            PXR_Enterprise.SwitchSystemFunction(SystemFunctionSwitchEnum.SFS_SECURITY_ZONE_PERMANENTLY,
                SwitchEnum.S_OFF);
        }
    }

    private void OnApplicationQuit()
    {
        CrashProbe.MarkCleanExit("Main.OnApplicationQuit");
    }
}
