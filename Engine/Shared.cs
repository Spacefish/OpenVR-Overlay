using Evergine.Bindings.Vulkan;

public unsafe partial class Engine {
    uint findMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);

        for (uint i = 0; i < memProperties.memoryTypeCount; i++)
        {
            ulong memoryType = ((uint)1) << (int)i;

            if ((typeFilter & memoryType) != 0) {
                var pointer = &memProperties.memoryTypes_0;
                var memortyType = *(pointer + sizeof(VkMemoryType) * i);

                if((memortyType.propertyFlags & properties) == properties) {
                    return i;
                }
            }
        }

        throw new Exception("Failed to find suitable memory type!");
    }

    void printMemoryTypes() {
        VkPhysicalDeviceMemoryProperties memProperties;
        VulkanNative.vkGetPhysicalDeviceMemoryProperties(physicalDevice, &memProperties);

        for (uint i = 0; i < memProperties.memoryTypeCount; i++)
        {
            var pointer = &memProperties.memoryTypes_0;
            var memortyType = *(pointer + sizeof(VkMemoryType) * i);
            Console.WriteLine($"Memory type {i}: {memortyType.propertyFlags}");
        }
        Console.WriteLine($"Memory type count: {memProperties.memoryTypeCount}");
    }
}