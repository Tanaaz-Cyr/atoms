using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

public class MPSphere
{
    private Vector3 _position;
    private Vector3 _velocity = Vector3.Zero; // Initialize with zero velocity
    private float _attractionStrength;
    private float _size;
    private Model? _sphereModel;
    private Microsoft.Xna.Framework.Color _color = Microsoft.Xna.Framework.Color.Red;
    private Vector3 _externalForce = Vector3.Zero;

    public Vector3 Position => _position;
    public float AttractionStrength => _attractionStrength;

    public MPSphere(Vector3 position, float attractionStrength, float size)
    {
        _position = position;
        _attractionStrength = attractionStrength;
        _size = size;
        _velocity = Vector3.Zero; // Ensure zero initial velocity
    }

    public void ApplyForce(Vector3 force)
    {
        _externalForce += force;
    }

    public void Update(GameTime gameTime, List<MPSphere> mpParticles, List<ESphere> eParticles)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Vector3 acceleration = Vector3.Zero;

        // Apply repulsion from other MP particles (inherited from E particles)
        foreach (var otherParticle in mpParticles)
        {
            if (otherParticle != this)
            {
                Vector3 direction = _position - otherParticle._position;
                float distance = direction.Length();
                if (distance < 0.1f) continue;

                // Calculate inherited repulsion from nearby E particles
                float inheritedRepulsion = 0;
                foreach (var eParticle in eParticles)
                {
                    float eDistance = Vector3.Distance(_position, eParticle.Position);
                    if (eDistance < 0.1f) continue;
                    
                    // Inherit 50% of E's repulsion force, scaled by distance
                    inheritedRepulsion += 1000.0f / (eDistance * eDistance); // 50% of E's repulsion (200.0f * 0.5)
                }

                // Apply the inherited repulsion force
                float force = inheritedRepulsion / (distance * distance);
                acceleration += direction * force;
            }
        }

        // Apply attraction from E particles (inverse square law)
        foreach (var eParticle in eParticles)
        {
            Vector3 direction = eParticle.Position - _position;
            float distance = direction.Length();
            if (distance < 0.1f) continue;

            // Inverse square law for attraction (base force)
            float force = AttractionStrength * 200.0f / (distance * distance);
            acceleration += direction * force;
        }

        // Update velocity and position with high damping for stability
        _velocity += acceleration * deltaTime * 0.01f; // Reduce force application to 1%
        _velocity *= 0.99f; // 99% damping to maintain stability
        _position += _velocity * deltaTime;

        // Keep particles within bounds
        float bounds = 2000f;
        _position = Vector3.Clamp(_position, new Vector3(-bounds), new Vector3(bounds));
    }

    public void UpdateSize(float newSize)
    {
        _size = newSize;
        _sphereModel = null; // Force recreation of the sphere model with new size
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect effect)
    {
        // Create a simple sphere mesh if we don't have one
        if (_sphereModel == null)
        {
            _sphereModel = CreateSphere(graphicsDevice, 0.5f * _size, 16);
        }

        // Draw the sphere
        foreach (var mesh in _sphereModel.Meshes)
        {
            foreach (var meshPart in mesh.MeshParts)
            {
                meshPart.Effect = effect;
                effect.DiffuseColor = new Vector3(1.0f, 0.0f, 0.0f); // Pure red color
                effect.EmissiveColor = new Vector3(0.8f, 0.0f, 0.0f); // Increased emissive red to make it glow more
                effect.World = Matrix.CreateScale(_size) * Matrix.CreateTranslation(_position);
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