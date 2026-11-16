// Rendering/IFrameRenderer.cs
using AsciiFarm.Core;

namespace AsciiFarm.Rendering;

public interface IFrameRenderer
{
    void Render(RenderFrame frame, World world);
}