using Evergine.Bindings.Vulkan;

public unsafe partial class Engine {

    private VkSurfaceKHR CreateWaylandSurface(VkInstance instance)
    {
        VkWaylandSurfaceCreateInfoKHR surfaceCreateInfo = new VkWaylandSurfaceCreateInfoKHR
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_WAYLAND_SURFACE_CREATE_INFO_KHR,
            display = (nint)null,
            surface = (nint)null
        };

        VkSurfaceKHR surface;
        Helpers.CheckErrors(VulkanNative.vkCreateWaylandSurfaceKHR(instance, &surfaceCreateInfo, null, &surface));
        return surface;
    }
}