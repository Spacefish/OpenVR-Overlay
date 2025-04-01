using Valve.VR;

public class VROverlay {
    private ulong overlayHandle;
    private ulong thumbnailHandle;

    private CVROverlay overlay => OpenVR.Overlay;

    public async Task Init() {
        var response = overlay.CreateOverlay(
            "com.example.overlay",
            "Example Overlay",
            ref overlayHandle
            // ref thumbnailHandle
        );
        if(response != EVROverlayError.None) {
            throw new Exception($"Failed to create overlay: {response}");
        }

        overlay.SetOverlayWidthInMeters(overlayHandle, 0.2f);
        overlay.SetOverlayInputMethod(overlayHandle, VROverlayInputMethod.Mouse);

        Task.Run(PollEventsTask);

        //overlay.SetOverlayColor(overlayHandle, 1.0f, 1.0f, 1.0f);
        // overlay.SetOverlayAlpha(overlayHandle, 0.5f);

        var texture = new Texture_t();
        texture.handle = IntPtr.Zero;
        texture.eType = ETextureType.IOSurface;
        texture.eColorSpace = EColorSpace.Auto;

        uint lastControllerId = uint.MaxValue;;
        for(int i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            var deviceClass = OpenVR.System.GetTrackedDeviceClass((uint)i);
            Console.WriteLine($"Device {i}: {deviceClass}");
            if (deviceClass == ETrackedDeviceClass.Controller)
            {
                Console.WriteLine($"Controller found: {i}");
                lastControllerId = (uint)i;
            }
        }

        HmdMatrix34_t transform = new HmdMatrix34_t();
        transform.m0 = 1.0f;
        transform.m1 = 0.0f;
        transform.m2 = 0.0f;
        transform.m3 = 0.0f;
        transform.m4 = 0.0f;
        transform.m5 = 1.0f;
        transform.m6 = 0.0f;
        transform.m7 = 0.0f;
        transform.m8 = 0.0f;
        transform.m9 = 0.0f;
        transform.m10 = 1.0f;
        transform.m11 = 0.0f;
        // overlay.GetOverlayTransformTrackedDeviceRelative(overlayHandle, ref lastControllerId, ref transform);
        
        var reply = overlay.SetOverlayTransformTrackedDeviceRelative(overlayHandle, lastControllerId, ref transform);
        if(reply != EVROverlayError.None)
        {
            Console.WriteLine($"Failed to set overlay transform: {reply}");
        }
        else
        {
            Console.WriteLine($"Overlay transform set successfully.");
        }
        Console.WriteLine($"Last Controller ID: {lastControllerId} Transform: {transform.m0} {transform.m1} {transform.m2} {transform.m3}");

        overlay.SetOverlayFlag(overlayHandle, VROverlayFlags.EnableControlBar | VROverlayFlags.EnableControlBarClose | VROverlayFlags.VisibleInDashboard, true);
        overlay.SetOverlayFromFile(overlayHandle, "/home/spacy/Downloads/480964138_990485743182183_2770213490742417694_n.jpg");
        // overlay.SetOverlayTexture(overlayHandle, ref texture);
        overlay.ShowOverlay(overlayHandle);
    }

    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

    private async Task PollEventsTask()
    {
        uint eventSize;
        unsafe
        {
            eventSize = (uint)sizeof(VREvent_t);
        }
        var token = cancellationTokenSource.Token;
        token.ThrowIfCancellationRequested();
        DateTime lastPulse = DateTime.MinValue;
        while(!token.IsCancellationRequested) {

            // main overlay
            VREvent_t vrEvent = new VREvent_t();
            if(overlay.PollNextOverlayEvent(overlayHandle, ref vrEvent, eventSize)) {
                var eventType = (EVREventType)vrEvent.eventType;

                switch(eventType)
                {
                    case EVREventType.VREvent_None:
                    case EVREventType.VREvent_Reserved_01:
                        break;
                    case EVREventType.VREvent_MouseMove:
                        if(lastPulse.AddMilliseconds(50) < DateTime.Now)
                        {
                            lastPulse = DateTime.Now;
                            OpenVR.System.TriggerHapticPulse(3, 1 , 300);
                        }
                        break;
                    default:
                        Console.WriteLine($"Event Type: {eventType}");
                        break;
                }
            }
            else {
                await Task.Delay(TimeSpan.FromMilliseconds(20), token);
            }
        }
    }
}