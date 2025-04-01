using Valve.VR;

VR.Init();

var overlay = new VROverlay();
overlay.Init();


Console.WriteLine("Press any key to exit...");
Console.Read();
Console.WriteLine("Exiting...");

VR.Shutdown();
