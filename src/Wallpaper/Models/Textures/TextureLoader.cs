using System.Drawing.Imaging;
using Renderer.Commands;
using Renderer.Resources;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Models;

public sealed class TextureLoader : IDisposable
{
    private readonly Dictionary<string, Texture> loadedTextures = [];
    private readonly Dictionary<int, Texture> loadedGltfImages = [];
    private readonly List<Texture> allTextures = [];

    private readonly ID3D12Device device;
    private readonly ImmediateCommandList commandList;
    private readonly string basePath;

    public TextureLoader(InitContext initContext, string basePath)
    {
        device = initContext.GraphicsContext.Device;
        commandList = initContext.CommandList;
        this.basePath = basePath;
    }

    public Texture? GetTextureFromFile(string? name, Format format)
    {
        if (name == null)
            return null;

        string fullpath = Path.GetFullPath(Path.Combine(basePath, name));
        if (loadedTextures.TryGetValue(fullpath, out var texture))
            return texture;

        return LoadTextureFromFile(fullpath, format);
    }

    public Texture? GetTextureFromGltfTexture(SharpGLTF.Schema2.Texture? gltfTexture, Format format)
    {
        var image = gltfTexture?.PrimaryImage;
        if (image == null)
            return null;

        int key = image.LogicalIndex;
        if (loadedGltfImages.TryGetValue(key, out var cached))
            return cached;

        return LoadTextureFromGltfImage(image, key, format);
    }

    private Texture? LoadTextureFromGltfImage(SharpGLTF.Schema2.Image image, int key, Format format)
    {
        var encoded = image.Content.Content;
        using var stream = new MemoryStream(encoded.ToArray());
        using var bitmap = new Bitmap(stream);

        var texture = ConvertTexture(bitmap, $"gltf_image_{key}",format);
        loadedGltfImages[key] = texture;
        return texture;
    }

    private Texture? LoadTextureFromFile(string fullpath, Format format)
    {
        if (!File.Exists(fullpath))
            return null;

        using var bitmap = new Bitmap(fullpath);
        var texture = ConvertTexture(bitmap, fullpath, format);
        loadedTextures[fullpath] = texture;
        return texture;
    }

    private Texture ConvertTexture(Bitmap bitmap, string name, Format format)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var pixels = LoadBitmapPixelsRgba8(bitmap);

        var textureResource = CreateTexture2D(device, width, height, format);
        using var uploadBuffer = CreateTextureUploadBuffer(device, pixels, width, height, 4);

        commandList.ExecuteImmediate(cmd =>
        {
            UploadTextureData(cmd, textureResource, uploadBuffer, width, height, format);
            cmd.ResourceBarrierTransition(textureResource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource);
        });

        var texture = new ImageTexture
        {
            Format = format,
            Name = name,
            TextureResource = textureResource,
            Width = width,
            Height = height
        };

        allTextures.Add(texture);
        return texture;
    }

    private static ID3D12Resource CreateTexture2D(ID3D12Device device, int width, int height, Format format)
    {
        var desc = ResourceDescription.Texture2D(
            format,
            (uint)width,
            (uint)height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: 1,
            sampleQuality: 0,
            flags: ResourceFlags.None);

        return device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            desc,
            ResourceStates.CopyDest);
    }

    private static byte[] LoadBitmapPixelsRgba8(Bitmap bitmap)
    {
        using var converted = new Bitmap(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(converted))
        {
            g.DrawImage(bitmap, 0, 0, bitmap.Width, bitmap.Height);
        }

        var rect = new Rectangle(0, 0, converted.Width, converted.Height);
        var data = converted.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

        try
        {
            int width = converted.Width;
            int height = converted.Height;
            var rgba = new byte[width * height * 4];

            unsafe
            {
                byte* srcBase = (byte*)data.Scan0;
                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcBase + y * data.Stride;
                    for (int x = 0; x < width; x++)
                    {
                        byte b = srcRow[x * 4 + 0];
                        byte g = srcRow[x * 4 + 1];
                        byte r = srcRow[x * 4 + 2];
                        byte a = srcRow[x * 4 + 3];
                        int dst = (y * width + x) * 4;
                        rgba[dst + 0] = r;
                        rgba[dst + 1] = g;
                        rgba[dst + 2] = b;
                        rgba[dst + 3] = a;
                    }
                }
            }

            return rgba;
        }
        finally
        {
            converted.UnlockBits(data);
        }
    }

    private static ID3D12Resource CreateTextureUploadBuffer(ID3D12Device device, byte[] rgbaPixels, int width, int height, int bytesPerPixel)
    {
        const int TextureDataPitchAlignment = 256;
        int sourceRowPitch = width * bytesPerPixel;
        int uploadRowPitch = Align(sourceRowPitch, TextureDataPitchAlignment);
        ulong uploadSize = (ulong)(uploadRowPitch * height);

        var uploadBuffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(uploadSize),
            ResourceStates.GenericRead);

        unsafe
        {
            void* mapped = null;
            uploadBuffer.Map(0, null, (nint*)&mapped);
            byte* dstBase = (byte*)mapped;

            fixed (byte* srcBase = rgbaPixels)
            {
                for (int y = 0; y < height; y++)
                {
                    byte* srcRow = srcBase + y * sourceRowPitch;
                    byte* dstRow = dstBase + y * uploadRowPitch;
                    Buffer.MemoryCopy(srcRow, dstRow, uploadRowPitch, sourceRowPitch);
                }
            }

            uploadBuffer.Unmap(0, null);
        }

        return uploadBuffer;
    }

    private static int Align(int value, int alignment)
    {
        return (value + alignment - 1) & ~(alignment - 1);
    }

    private static void UploadTextureData(ID3D12GraphicsCommandList cmd, ID3D12Resource texture, ID3D12Resource uploadBuffer, int width, int height, Format format)
    {
        const int TextureDataPitchAlignment = 256;
        int bytesPerPixel = 4;
        int sourceRowPitch = width * bytesPerPixel;
        int uploadRowPitch = Align(sourceRowPitch, TextureDataPitchAlignment);

        var footprint = new PlacedSubresourceFootPrint
        {
            Offset = 0,
            Footprint = new SubresourceFootPrint
            {
                Format = format,
                Width = (uint)width,
                Height = (uint)height,
                Depth = 1,
                RowPitch = (uint)uploadRowPitch
            }
        };

        var src = new TextureCopyLocation(uploadBuffer, footprint);
        var dst = new TextureCopyLocation(texture, 0);
        cmd.CopyTextureRegion(dst, 0, 0, 0, src, null);
    }

    public void Dispose()
    {
        foreach (var texture in allTextures)
        {
            texture.Dispose();
        }

        allTextures.Clear();
        loadedTextures.Clear();
        loadedGltfImages.Clear();
    }
}
