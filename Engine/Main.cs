using Evergine.Bindings.Vulkan;

public partial class Engine {

    public VkInstance Instance => instance;
    public VkPhysicalDevice PhysicalDevice => physicalDevice;
    public VkDevice Device => device;
    public VkQueue GraphicsQueue => graphicsQueue;
    public uint GraphicsQueueFamilyIndex => graphicsQueueFamilyIndex;
    public void Init()
    {
        CreateInstance();
        // var surface = CreateWaylandSurface(instance);
        //var texture = LoadTexture("/home/spacy/Pictures/GkGCJ7CXsAAIGMX.jpeg");
        // Console.WriteLine($"Texture loaded: {texture.Handle}");
    }
}