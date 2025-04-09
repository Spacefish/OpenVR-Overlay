using System.Reflection;
using System.Threading.Tasks;
using Evergine.Bindings.Vulkan;
using Valve.VR;

public class VROverlay : IDisposable
{
    private ulong overlayHandle;
    // private ulong thumbnailHandle;

    CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    Task? eventHandlerTask;

    private CVROverlay overlay => OpenVR.Overlay;

    public void Init() {
        if(eventHandlerTask != null) {
            throw new InvalidOperationException("Overlay already initialized.");
        }
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

        eventHandlerTask = Task.Run(PollEventsTask);

        FixOverlayToController();

        Console.WriteLine($"Before load texture");
        LoadOverLayTextureAndSet();
        
        Console.WriteLine($"Before Show Overlay");
        overlay.ShowOverlay(overlayHandle);
    }


    private void LoadOverLayTextureAndSet() {
        
        var engine = new Engine();
        engine.Init();

        var texturePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                ?? throw new Exception("no entry assembly location"),
            "test_texture.jpg"
        );
        var vkTexture = engine.LoadTexture(texturePath);

        SetOverlayTexture(engine, vkTexture);
    }

    private void SetOverlayTexture(Engine engine, VkImage vkTexture) {
        VRVulkanTextureData_t vulkanTextureDataInfo = new VRVulkanTextureData_t {
            m_nImage = vkTexture.Handle, // 0x00
            m_pDevice = engine.Device.Handle, // 0x08
            m_pPhysicalDevice = engine.PhysicalDevice.Handle, // 0x10
            m_pInstance = engine.Instance.Handle, // 0x18
            m_pQueue = engine.GraphicsQueue.Handle, // 0x20
            m_nQueueFamilyIndex = engine.GraphicsQueueFamilyIndex, // 0x28
            m_nWidth = 512, // 0x2C
            m_nHeight = 512, // 0x30
            m_nFormat = (uint)EVRRenderModelTextureFormat.RGBA8_SRGB, // 0x34
            m_nSampleCount = 1, // 0x38
        };
        unsafe {
            Console.WriteLine($"nint: {sizeof(nint)} uint: {sizeof(uint)} ulong: {sizeof(ulong)}");
            var structSize = sizeof(VRVulkanTextureData_t);
            Console.WriteLine($"Struct size: {structSize}");
        }
        unsafe {
            var texture = new Texture_t() {
                handle = (nint)(&vulkanTextureDataInfo),
                eType = ETextureType.Vulkan,
                eColorSpace = EColorSpace.Linear,
            };
            Console.WriteLine($"Texture: {texture.handle} {texture.eType} {texture.eColorSpace}");

            // CVROverlayLatest::SetOverlayTextureXR

            var error = overlay.SetOverlayTexture(overlayHandle, ref texture);
            if(error != EVROverlayError.None) {
                throw new Exception($"Failed to set overlay texture: {error}");
            }
        }
    }

    private void FixOverlayToController()
    {
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

        // identity transform
        HmdMatrix34_t transform = new HmdMatrix34_t() {
            m0 = 1.0f,
            m1 = 0.0f,
            m2 = 0.0f,
            m3 = 0.0f,
            m4 = 0.0f,
            m5 = 1.0f,
            m6 = 0.0f,
            m7 = 0.0f,
            m8 = 0.0f,
            m9 = 0.0f,
            m10 = 1.0f,
            m11 = 0.0f
        };
        
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
    }

    private void SetTestImageAsOverlay()
    {
        var imagePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location)
                ?? throw new Exception("no entry assembly location"),
            "testimage.jpg"
        );
        if(!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        overlay.SetOverlayFromFile(overlayHandle, imagePath);
    }

    private async Task PollEventsTask()
    {
        uint eventSize;
        unsafe
        {
            eventSize = (uint)sizeof(VREvent_t);
        }
        var token = cancellationTokenSource.Token;

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

    public void Dispose()
    {
        overlay.HideOverlay(overlayHandle);
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        
        if(eventHandlerTask != null)
        {
            eventHandlerTask.Wait();
        }
    }
}