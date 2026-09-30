using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Applies operator settings from a file the pipeline can push, so the headset never has to raise a
/// keyboard. This exists because the built-in dialog cannot open one on this device: the app asks for
/// the soft keyboard as a client on display 0 while its VR window lives on display 26, and Android
/// discards the request outright
/// ("isInputMethodClientFocus: display ID mismatch. from client: 0 from window: 26" /
///  "Ignoring showSoftInput of uid 10128"). The address field is therefore unreachable in the UI, and
/// the value it would have written lives in PlayerPrefs, which is not readable or writable from adb
/// without root.
///
/// Drop a JSON file at &lt;persistentDataPath&gt;/remote_vision.json — on this device
/// /storage/emulated/0/Android/data/com.xrobotoolkit.client/files/remote_vision.json, the same
/// directory as video_source.yml — containing:
///
///     {"videoSource": "ZEDMINI", "address": "10.33.88.161"}
///
/// Both fields are optional; whatever is present is applied through the same store the dialog uses,
/// so it takes effect exactly as an operator entry would, and the result is logged for logcat.
/// </summary>
[Serializable]
public class RemoteVisionOverrides
{
    public string videoSource;
    public string address;
}

public static class RemoteVisionBootstrap
{
    public const string FileName = "remote_vision.json";

    public static void ApplyOperatorOverrides()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            if (!File.Exists(path))
            {
                Debug.Log("[RemoteVisionBootstrap] no " + path + "; leaving the operator settings alone");
                return;
            }

            string text = File.ReadAllText(path);
            RemoteVisionOverrides overrides = JsonUtility.FromJson<RemoteVisionOverrides>(text);
            if (overrides == null || string.IsNullOrWhiteSpace(overrides.address))
            {
                Debug.Log("[RemoteVisionBootstrap] " + path + " has no address; ignoring it");
                return;
            }

            string source = string.IsNullOrWhiteSpace(overrides.videoSource) ? null : overrides.videoSource;
            if (!RemoteVisionAddressStore.TrySave(source, overrides.address, out string normalized))
            {
                Debug.LogError("[RemoteVisionBootstrap] address rejected by the normaliser: "
                               + overrides.address);
                return;
            }

            PlayerPrefs.Save();
            Debug.Log("[RemoteVisionBootstrap] applied address " + normalized
                      + " for video source " + (source ?? "(last used)"));
        }
        catch (Exception error)
        {
            // Never take the app down over a bootstrap file: the operator can still use the dialog.
            Debug.LogError("[RemoteVisionBootstrap] failed to apply " + path + ": " + error.Message);
        }
    }
}
