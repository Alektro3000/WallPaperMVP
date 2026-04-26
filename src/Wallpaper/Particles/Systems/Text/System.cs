
using System.Numerics;
using Particles.Core;
using Particles.Resources;
using Particles.Settings;
using Renderer.FrameManagement;
using Renderer.Resources;
using Vortice.Direct3D12;

namespace Particles.Systems.Text;


[Shader("text\\compute.hlsl", "cs")]
[Shader("text\\precompute.hlsl", "cs")]
public class ParticleSystem : BaseParticleSystem, IParticleSystem<Settings>
{
    protected Controller Controller;
    Random random = new Random();
    public ParticleSystem(
        ParticleSystemInitContext context, Settings settings)
    {
        ConstantKey = context.Registry.Reserve(device => BufferFactory.CreateConstantBuffer<Constants>(device, "TextSystem_Constant"));
        ConstructRequiredFields(context, generateParticles(settings.initSettings), "TextSystem", "text/compute.hlsl", "text/precompute.hlsl");
        Controller = new Controller(ParticleBuffers);
    }

    [SystemBuilder]
    public static ParticleSystem? Create(ParticleSystemInitContext context, Settings settings)
    {
        if(settings.initSettings.MaxParticleAmount <= 0)
            return null;
        return new ParticleSystem(context, settings);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource, SystemSettings systemSettings)
    {
        Controller.UpdateConstantBuffer(
            ref currentResource.GetBufferConstantRef<Constants>(ConstantKey),
            currentResource.frameMetric, systemSettings);
    }
    private Point GetRandomWhitePixelWeightedTop(Bitmap bitmap)
    {
        const int MaxAttempts = 100;

        for (int attempt = 0; attempt < MaxAttempts; attempt++)
        {
            // Strong bias toward the top:
            // if u is uniform [0,1), then u^2 biases toward 0
            double u = random.NextDouble();
            int y = (int)(bitmap.Height * u);

            int x = random.Next(bitmap.Width);

            Color c = bitmap.GetPixel(x, y);

            // Accept only white pixels
            if (c.R > 240 && c.G > 240 && c.B > 240)
                return new Point(x, y);
        }

        // Fallback: exhaustive search with explicit weighting
        List<(Point point, double weight)> candidates = new();

        for (int y = 0; y < bitmap.Height; y++)
        {
            // Weight decreases toward bottom.
            // Top row has weight 1.0, bottom row ~0.1
            double t = (double)y / (bitmap.Height - 1);
            double weight = 1.0 - 0.9 * t;

            for (int x = 0; x < bitmap.Width; x++)
            {
                Color c = bitmap.GetPixel(x, y);
                if (c.R > 240 && c.G > 240 && c.B > 240)
                    candidates.Add((new Point(x, y), weight));
            }
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException("Bitmap contains no white pixels.");

        double totalWeight = candidates.Sum(c => c.weight);
        double r = random.NextDouble() * totalWeight;

        foreach (var candidate in candidates)
        {
            r -= candidate.weight;
            if (r <= 0)
                return candidate.point;
        }

        return candidates[^1].point;
    }
    private Bitmap GenerateBitmap(InitSettings settings)
    {
        string text = settings.Text;

        var bitmap = new Bitmap((int)settings.Resolution.X, (int)settings.Resolution.Y);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.Black);

        using var font = new Font(settings.Font, settings.TextSize, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var format = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(text, font, brush, new RectangleF(0,0,bitmap.Width,bitmap.Height), format);
        return bitmap;
    }
    private Particle[] generateParticles(InitSettings settings)
    {
        using var text = GenerateBitmap(settings);
        float size = settings.PixelSize;
        Vector2 centerPos = settings.CenterPos;

        Vector2 bitmapSize = new Vector2(text.Width, text.Height); 
        Vector2 size2d = size * bitmapSize;
        Vector2 StartPos = centerPos - size2d*0.5f;
        Vector2 ParticleScaling = new Vector2(size,-size);
        return Enumerable.Range(0, (int)settings.MaxParticleAmount)
                .Select( x => GetRandomWhitePixelWeightedTop(text))
                .Select( pos => 
            new Particle()
            {
                CustomData = StartPos + new Vector2(pos.X, pos.Y) * ParticleScaling  
            }).ToArray();
    }

}