
using System.Numerics;
using Vortice.Direct3D12;

[Shader("text\\compute.hlsl", "cs")]
[Shader("text\\precompute.hlsl", "cs")]
public class TextSystem : ParticleSystem
{
    protected TextController ParticleSystemController;
    public TextSystem(
        InitContext context)
    {
        ConstructRequiredFields(context, generateParticles(), "text/compute.hlsl", "text/precompute.hlsl");
        ParticleSystemController = new TextController(ParticleBuffers);
    }

    public override void UpdateConstantBuffers(FrameResource currentResource)
    {
        ParticleSystemController.UpdateConstantBuffer(
            ref currentResource.GetBufferConstantRef<TextConstants>(ConstantKey),
            currentResource.frameMetric);
    }
    public override void InitBuffer(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(ConstantKey,BufferHelper.CreateConstantBuffer<TextConstants>(device));
    }
    Random random = new Random();
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
    private Bitmap GenerateBitmap()
    {
        string text = "Встречая страх, создавай будущее";

        var bitmap = new Bitmap(900, 120);
        using var g = Graphics.FromImage(bitmap);

        g.Clear(Color.Black);

        using var font = new Font("Arial", 24, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        var format = new StringFormat()
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        g.DrawString(text, font, brush, new RectangleF(0,0,bitmap.Width,bitmap.Height),  format);
        return bitmap;
    }
    private Particle[] generateParticles()
    {
        using var text = GenerateBitmap();
        float size = 0.0028f;
        Vector2 centerPos = new Vector2(0f, -0.4f);

        Vector2 bitmapSize = new Vector2(text.Width, text.Height); 
        Vector2 size2d = size * bitmapSize;
        Vector2 StartPos = centerPos - size2d*0.5f;
        Vector2 ParticleScaling = new Vector2(size,-size);
        return Enumerable.Range(0, 4096)
                .Select( x => GetRandomWhitePixelWeightedTop(text))
                .Select( pos => 
            new Particle()
            {
                CustomData = StartPos + new Vector2(pos.X, pos.Y) * ParticleScaling  
            }).ToArray();
    }
}