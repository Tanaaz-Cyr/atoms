using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class ESphere
{
    private Vector3 _position;
    private Vector3 _velocity = Vector3.Zero;
    private float _repulsionStrength;
    private Model? _sphereModel;
    private Microsoft.Xna.Framework.Color _color = Microsoft.Xna.Framework.Color.Yellow;
    private float _size = 0.1f; // Restored to previous size

    public Vector3 Position => _position;
    public Vector3 Velocity => _velocity;

    public ESphere(Vector3 position, float repulsionStrength)
    {
        _position = position;
        _repulsionStrength = repulsionStrength;
        _velocity = Vector3.Zero;
        _size = 0.1f; // Initialize size
    }

    public void Update(GameTime gameTime, List<ESphere> eParticles, List<MPSphere> mpParticles)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 acceleration = Vector3.Zero;

        // Apply repulsion from other E particles (inverse square law, 0.1x of MP attraction)
        foreach (var otherParticle in eParticles)
        {
            if (otherParticle != this)
            {
                Vector3 direction = _position - otherParticle._position;
                float distance = direction.Length();
                if (distance < 0.1f) continue;

                // Inverse square law for repulsion (0.1x of MP attraction)
                float force = _repulsionStrength * 2.0f / (distance * distance); // Keep at 1/10 (2.0f)
                acceleration += direction * force;
            }
        }

        // Apply attraction from MP particles (inverse square law)
        foreach (var mpParticle in mpParticles)
        {
            Vector3 direction = mpParticle.Position - _position;
            float distance = direction.Length();
            if (distance < 0.1f) continue;

            // Inverse square law for attraction (base force)
            float force = mpParticle.AttractionStrength * 200.0f / (distance * distance); // Restored to original (200.0f)
            acceleration += direction * force;
        }

        // Update velocity and position with less damping for faster movement
        _velocity += acceleration * deltaTime * 2.0f;
        _velocity *= 0.99f; // 99% of velocity is maintained (1% damping)
        _position += _velocity * deltaTime;

        // Keep particles within bounds
        float bounds = 2000f; // Increased from 20f to 2000f
        _position = Vector3.Clamp(_position, new Vector3(-bounds), new Vector3(bounds));
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        // Create a simple sphere mesh if we don't have one
        if (_sphereModel == null)
        {
            _sphereModel = CreateSphere(graphicsDevice, _size, 16);
        }

        // Set up world matrix for this sphere
        effect.World = Matrix.CreateTranslation(_position);
        effect.DiffuseColor = _color.ToVector3();
        effect.EmissiveColor = _color.ToVector3() * 0.5f; // Add some glow effect

        // Draw the sphere
        foreach (var mesh in _sphereModel.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                meshPart.Effect = effect;
            }
            mesh.Draw();
        }
    }

    private Model CreateSphere(GraphicsDevice graphicsDevice, float radius, int tessellation)
    {
        var vertices = new List<VertexPositionNormalTexture>();
        var indices = new List<short>();

        // Create vertices
        for (int i = 0; i <= tessellation; i++)
        {
            float v = 1 - (float)i / tessellation;
            float latitude = (i * MathHelper.Pi) / tessellation;

            for (int j = 0; j <= tessellation; j++)
            {
                float u = (float)j / tessellation;
                float longitude = (j * 2 * MathHelper.Pi) / tessellation;

                float x = (float)(Math.Sin(latitude) * Math.Cos(longitude));
                float y = (float)Math.Cos(latitude);
                float z = (float)(Math.Sin(latitude) * Math.Sin(longitude));

                Vector3 normal = new Vector3(x, y, z);
                Vector3 position = normal * radius;

                vertices.Add(new VertexPositionNormalTexture(position, normal, new Vector2(u, v)));
            }
        }

        // Create indices
        for (int i = 0; i < tessellation; i++)
        {
            for (int j = 0; j < tessellation; j++)
            {
                int topLeft = i * (tessellation + 1) + j;
                int topRight = topLeft + 1;
                int bottomLeft = (i + 1) * (tessellation + 1) + j;
                int bottomRight = bottomLeft + 1;

                indices.Add((short)topLeft);
                indices.Add((short)bottomLeft);
                indices.Add((short)topRight);

                indices.Add((short)topRight);
                indices.Add((short)bottomLeft);
                indices.Add((short)bottomRight);
            }
        }

        // Create vertex and index buffers
        var vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), vertices.Count, BufferUsage.WriteOnly);
        vertexBuffer.SetData(vertices.ToArray());

        var indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices.ToArray());

        // Create model mesh part
        var meshPart = new ModelMeshPart
        {
            VertexBuffer = vertexBuffer,
            IndexBuffer = indexBuffer,
            NumVertices = vertices.Count,
            StartIndex = 0,
            PrimitiveCount = indices.Count / 3
        };

        // Create model mesh
        var modelMesh = new ModelMesh(graphicsDevice, new List<ModelMeshPart> { meshPart });
        var model = new Model(graphicsDevice, new List<ModelBone>(), new List<ModelMesh> { modelMesh });

        return model;
    }
} 