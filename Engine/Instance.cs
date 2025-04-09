using System.Runtime.InteropServices;
using Evergine.Bindings.Vulkan;

public unsafe partial class Engine {

    private VkInstance instance;
    private VkPhysicalDevice physicalDevice;
    private VkDevice device;
    private VkQueue graphicsQueue;

    private uint graphicsQueueFamilyIndex = 0;


    string[] requiredExtensions = new[]
    {
        "VK_KHR_surface",
        "VK_KHR_wayland_surface",
        // "VK_KHR_win32_surface",
        "VK_EXT_debug_utils",
        "VK_KHR_external_memory_capabilities"
    };

    private void CreateInstance() {
        VkApplicationInfo appInfo = new VkApplicationInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_APPLICATION_INFO,
            pApplicationName = "Hello Vulkan".ToPointer(),
            applicationVersion = Helpers.Version(1, 0, 0),
            pEngineName = "No Engine".ToPointer(),
            engineVersion = Helpers.Version(1, 0, 0),
            apiVersion = Helpers.Version(1, 3, 0)
        };

        VkInstanceCreateInfo createInfo = default;
        createInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_INSTANCE_CREATE_INFO;
        createInfo.pApplicationInfo = &appInfo;

        var availableExtensions = GetAllInstanceExtensions();

        var missingExtensions = requiredExtensions.Except(availableExtensions.Select(e => e.extensionName))
            .ToArray();

        if (missingExtensions.Length > 0)
            throw new InvalidOperationException($"Missing required Vulkan extensions: {string.Join(", ", missingExtensions)}");

        // register extensions we need
        IntPtr* extensionsToBytesArray = stackalloc IntPtr[requiredExtensions.Length];
        for (int i = 0; i < requiredExtensions.Length; i++)
        {
            extensionsToBytesArray[i] = (nint)requiredExtensions[i].ToPointer();
        }
        createInfo.enabledExtensionCount = (uint)requiredExtensions.Length;
        createInfo.ppEnabledExtensionNames = (byte**)extensionsToBytesArray;
        createInfo.enabledLayerCount = 1;
        IntPtr* layers = stackalloc IntPtr[1];
        layers[0] = (nint)"VK_LAYER_KHRONOS_validation".ToPointer();
        createInfo.ppEnabledLayerNames = (byte**)layers;

        var x = Helpers.GetString((byte*)layers[0]);

        // create instance
        VkInstance instance;
        Helpers.CheckErrors(VulkanNative.vkCreateInstance(&createInfo, null, &instance));
        this.instance = instance;

        // find physical device
        uint physicalDeviceCount;
        Helpers.CheckErrors(VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, null));
        VkPhysicalDevice* physicalDevices = stackalloc VkPhysicalDevice[(int)physicalDeviceCount];
        Helpers.CheckErrors(VulkanNative.vkEnumeratePhysicalDevices(instance, &physicalDeviceCount, physicalDevices));
        physicalDevice = physicalDevices[0];
        for(int i = 0; i < physicalDeviceCount; i++)
        {
            var deviceProperties = new VkPhysicalDeviceProperties();
            VulkanNative.vkGetPhysicalDeviceProperties(physicalDevices[i], &deviceProperties);
            Console.WriteLine($"Device {i}: {Helpers.GetString(deviceProperties.deviceName)} - {deviceProperties.deviceType}");
            if (deviceProperties.deviceType == VkPhysicalDeviceType.VK_PHYSICAL_DEVICE_TYPE_DISCRETE_GPU)
            {
                physicalDevice = physicalDevices[i];
                break;
            }

        }
        
        uint queueFamilyCount = 0;
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, null);

        VkQueueFamilyProperties* queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
        VulkanNative.vkGetPhysicalDeviceQueueFamilyProperties(physicalDevice, &queueFamilyCount, queueFamilies);

        for (uint i = 0; i < queueFamilyCount; i++)
        {
            if ((queueFamilies[i].queueFlags & VkQueueFlags.VK_QUEUE_GRAPHICS_BIT) != 0)
            {
                graphicsQueueFamilyIndex = i;
                break;
            }
        }

        float* queuePriorities = stackalloc float[1];
        queuePriorities[0] = 1.0f;

        VkDeviceQueueCreateInfo queueCreateInfo;
        queueCreateInfo.sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_QUEUE_CREATE_INFO;
        queueCreateInfo.queueFamilyIndex = graphicsQueueFamilyIndex;
        queueCreateInfo.queueCount = 1;
        queueCreateInfo.pQueuePriorities = queuePriorities;

        string[] requiredDeviceExtensions = [
            "VK_KHR_external_semaphore",
            "VK_KHR_external_semaphore_fd",
            "VK_KHR_external_memory",
            "VK_KHR_external_memory_fd",
            "VK_KHR_get_memory_requirements2"
        ];

        IntPtr* deviceExtensions = stackalloc IntPtr[requiredDeviceExtensions.Length];
        for (int i = 0; i < requiredDeviceExtensions.Length; i++)
        {
            deviceExtensions[i] = (nint)requiredDeviceExtensions[i].ToPointer();
        }

        VkDeviceCreateInfo deviceCreateInfo = new VkDeviceCreateInfo
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_DEVICE_CREATE_INFO,
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &queueCreateInfo,
            enabledExtensionCount = (uint)requiredDeviceExtensions.Length,
            ppEnabledExtensionNames = (byte**)deviceExtensions,
            pEnabledFeatures = null
        };
        VkDevice device;
        Helpers.CheckErrors(VulkanNative.vkCreateDevice(physicalDevice, &deviceCreateInfo, null, &device));
        this.device = device;

        VkQueue graphicsQueue;
        VulkanNative.vkGetDeviceQueue(device, graphicsQueueFamilyIndex, 0, &graphicsQueue);
        this.graphicsQueue = graphicsQueue;
    }

    private IEnumerable<(string extensionName, uint specVersion)> GetAllInstanceExtensions()
    {
        uint extensionCount;
        Helpers.CheckErrors(VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, null));
        VkExtensionProperties* extensions = stackalloc VkExtensionProperties[(int)extensionCount];
        Helpers.CheckErrors(VulkanNative.vkEnumerateInstanceExtensionProperties(null, &extensionCount, extensions));

        var result = new (string extensionName, uint specVersion)[extensionCount];
        for (int i = 0; i < extensionCount; i++)
        {
            result[i].extensionName = Helpers.GetString(extensions[i].extensionName);
            result[i].specVersion = extensions[i].specVersion;
        }
        return result;
    }
}