using Valve.VR;

[Flags]
public enum VrState {
	NotInitialized = 1,
	OK = 2,
	HeadsetNotDetected = 4,
	UnknownError = 8
}

public static class VR
{
    public static VrState State { get; private set; } = VrState.NotInitialized;
    public static CVRSystem? CVR { get; private set; }

    public static void Init(EVRApplicationType appType = EVRApplicationType.VRApplication_Overlay) {
        if(!State.HasFlag(VrState.NotInitialized)) {
            throw new InvalidOperationException("VR is already initialized.");
        }
        EVRInitError error = EVRInitError.None;
        CVR = OpenVR.Init( ref error, appType );
        if (error != EVRInitError.None) {
            State = State | VrState.UnknownError;
            throw new Exception($"OpenVR.Init failed with error: {error}");
        }
    }

    public static void Shutdown() {
        if (CVR != null) {
            OpenVR.Shutdown();
            CVR = null;
            State = VrState.NotInitialized;
        }
    }
}