using Evergine.Bindings.Vulkan;
using Humanizer;

public unsafe partial class Engine {
    private void LoadTexture()
    {
        // Load texture from file
        string filePath = "path/to/your/texture.png";
        var texture = LoadTexture(filePath);

        // Use the texture in your rendering pipeline
        // ...
    }
    
    public VkImage LoadTexture(string filePath)
    {
        // Load the image data from the file
        byte[] imageData = File.ReadAllBytes(filePath);
        int width = 512; // Replace with actual width
        int height = 512; // Replace with actual height

        VkImage textureImage;
        VkDeviceMemory textureImageMemory;

        // Create a Vulkan image
        VkImageCreateInfo imageCreateInfo = new VkImageCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_IMAGE_CREATE_INFO,
            imageType = VkImageType.VK_IMAGE_TYPE_2D,
            format = VkFormat.VK_FORMAT_R8G8B8A8_SRGB,
            extent = new VkExtent3D { width = (uint)width, height = (uint)height, depth = 1 },
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.VK_SAMPLE_COUNT_1_BIT,
            tiling = VkImageTiling.VK_IMAGE_TILING_LINEAR,
            usage = VkImageUsageFlags.VK_IMAGE_USAGE_TRANSFER_DST_BIT | VkImageUsageFlags.VK_IMAGE_USAGE_SAMPLED_BIT,
            sharingMode = VkSharingMode.VK_SHARING_MODE_CONCURRENT,
            initialLayout = VkImageLayout.VK_IMAGE_LAYOUT_UNDEFINED
        };

        Helpers.CheckErrors(VulkanNative.vkCreateImage(device, &imageCreateInfo, null, &textureImage));

        // get memorry requirements
        VkMemoryRequirements memRequirements;
        VulkanNative.vkGetImageMemoryRequirements(device, textureImage, &memRequirements);
        
        Console.WriteLine($"Texture size: {Humanizer.Bytes.ByteSize.FromBytes(memRequirements.size).Humanize()}");;
        
        // printMemoryTypes();

        VkMemoryAllocateInfo allocInfo;
        allocInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_MEMORY_ALLOCATE_INFO;
        allocInfo.allocationSize = memRequirements.size;
        allocInfo.memoryTypeIndex = findMemoryType(memRequirements.memoryTypeBits, VkMemoryPropertyFlags.VK_MEMORY_PROPERTY_DEVICE_LOCAL_BIT); // find memory type which is GPU local

        Helpers.CheckErrors(VulkanNative.vkAllocateMemory(device, &allocInfo, null, &textureImageMemory));

        Helpers.CheckErrors(VulkanNative.vkBindImageMemory(device, textureImage, textureImageMemory, 0));

        return textureImage;
    }

    

}