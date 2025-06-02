using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

public class SphereSimulation : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private BasicEffect _basicEffect = null!;
    private List<ESphere> _eSpheres = null!;
    private List<MPSphere> _mpSpheres = null!;
    private SpriteFont _font = null!;
    private Random _random = new Random();

    // Simulation parameters
    private int _numESpheres = 10;
    private int _numMPSpheres = 2;
    private float _eSphereSize = 0.1f;
    private float _mpSphereSize = 1.5f;
    private float _repulsionStrength = 5.0f;
    private float _attractionStrength = 3.0f;
    private const int MAX_E_PARTICLES = 1000;
    private const int MAX_MP_PARTICLES = 100;

    // Camera parameters
    private Vector3 _cameraPosition = new Vector3(0, 0, 20);
    private Vector3 _cameraTarget = Vector3.Zero;
    private Vector3 _cameraUp = Vector3.Up;
    private float _cameraRotation = 0f;
    private float _cameraDistance = 40f;
    private MouseState _previousMouseState;
    private bool _isPanning = false;
    private Vector2 _lastMousePosition;
    private float _panSpeed = 0.1f;

    private KeyboardState _previousKeyboardState;

    public static void Main()
    {
        using (var game = new SphereSimulation())
        {
            game.Run();
        }
    }

    public SphereSimulation()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "Sphere Simulation";
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.ApplyChanges();

        // Enable depth testing
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        _eSpheres = new List<ESphere>();
        _mpSpheres = new List<MPSphere>();

        SpawnSpheres();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _basicEffect = new BasicEffect(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Font");

        // Set up basic effect
        _basicEffect.EnableDefaultLighting();
        _basicEffect.PreferPerPixelLighting = true;
        _basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45f),
            GraphicsDevice.Viewport.AspectRatio,
            0.1f,
            1000f);
    }

    private void SpawnSpheres()
    {
        _eSpheres.Clear();
        _mpSpheres.Clear();

        // Spawn E spheres
        for (int i = 0; i < _numESpheres; i++)
        {
            Vector3 position = new Vector3(
                (float)(_random.NextDouble() * 40 - 20),
                (float)(_random.NextDouble() * 40 - 20),
                (float)(_random.NextDouble() * 40 - 20)
            );
            _eSpheres.Add(new ESphere(position, _repulsionStrength));
        }

        // Spawn MP spheres
        for (int i = 0; i < _numMPSpheres; i++)
        {
            Vector3 position = new Vector3(
                (float)(_random.NextDouble() * 40 - 20),
                (float)(_random.NextDouble() * 40 - 20),
                (float)(_random.NextDouble() * 40 - 20)
            );
            _mpSpheres.Add(new MPSphere(position, _attractionStrength, _mpSphereSize));
        }
    }

    protected override void Update(GameTime gameTime)
    {
        // Update E spheres
        foreach (var eSphere in _eSpheres)
        {
            eSphere.Update(gameTime, _eSpheres, _mpSpheres);
        }

        // Update MP spheres
        foreach (var mpSphere in _mpSpheres)
        {
            mpSphere.Update(gameTime, _mpSpheres, _eSpheres);
        }

        // Handle input
        HandleInput();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.CornflowerBlue);

        // Update view matrix
        _basicEffect.View = Matrix.CreateLookAt(_cameraPosition, _cameraTarget, _cameraUp);

        // Draw 3D objects
        foreach (var pass in _basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();

            // Combine and sort all spheres by Z-depth
            var allSpheres = _mpSpheres.Cast<object>().Concat(_eSpheres.Cast<object>())
                .Select(s => new
                {
                    Sphere = s,
                    ZDepth = s is MPSphere mp ? mp.Position.Z : ((ESphere)s).Position.Z
                })
                .OrderBy(s => s.ZDepth)
                .ToList();

            // Draw all spheres in order of Z-depth
            foreach (var sphere in allSpheres)
            {
                if (sphere.Sphere is MPSphere mp)
                    mp.Draw(GraphicsDevice, _basicEffect);
                else
                    ((ESphere)sphere.Sphere).Draw(GraphicsDevice, _basicEffect);
            }
        }

        // Draw UI
        _spriteBatch.Begin();
        _spriteBatch.DrawString(_font, $"E Particles: {_numESpheres}/{MAX_E_PARTICLES} (Ctrl+1: Add, Ctrl+2: Remove Random)", new Vector2(10, 10), Microsoft.Xna.Framework.Color.White);
        _spriteBatch.DrawString(_font, $"MP Particles: {_numMPSpheres}/{MAX_MP_PARTICLES} (Ctrl+3: Add, Ctrl+4: Remove Random)", new Vector2(10, 30), Microsoft.Xna.Framework.Color.White);
        _spriteBatch.DrawString(_font, $"Reset: Ctrl+R", new Vector2(10, 50), Microsoft.Xna.Framework.Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void AddESphere()
    {
        if (_eSpheres.Count >= 100) return;

        // Find a random MP particle to spawn near
        if (_mpSpheres.Count > 0)
        {
            var targetMP = _mpSpheres[_random.Next(_mpSpheres.Count)];
            
            // Generate a random offset within a small radius (0.5 units) from the MP particle
            Vector3 offset = new Vector3(
                (float)(_random.NextDouble() - 0.5) * 1.0f,
                (float)(_random.NextDouble() - 0.5) * 1.0f,
                (float)(_random.NextDouble() - 0.5) * 1.0f
            );
            
            // Spawn the E particle near the MP particle
            Vector3 position = targetMP.Position + offset;
            _eSpheres.Add(new ESphere(position, 1.0f));
            Console.WriteLine($"Added E particle near MP particle. Total E particles: {_eSpheres.Count}");
        }
        else
        {
            // If no MP particles exist, spawn at a random position
            Vector3 position = new Vector3(
                (float)(_random.NextDouble() - 0.5) * 10,
                (float)(_random.NextDouble() - 0.5) * 10,
                (float)(_random.NextDouble() - 0.5) * 10
            );
            _eSpheres.Add(new ESphere(position, 1.0f));
            Console.WriteLine($"Added E particle at random position. Total E particles: {_eSpheres.Count}");
        }
    }

    private void HandleInput()
    {
        var keyboardState = Keyboard.GetState();
        var previousKeyboardState = _previousKeyboardState;
        var mouseState = Mouse.GetState();
        var previousMouseState = _previousMouseState;

        // Handle mouse panning
        if (mouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
        {
            if (!_isPanning)
            {
                _isPanning = true;
                _lastMousePosition = new Vector2(mouseState.X, mouseState.Y);
            }
            else
            {
                Vector2 currentMousePosition = new Vector2(mouseState.X, mouseState.Y);
                Vector2 delta = currentMousePosition - _lastMousePosition;
                
                // Calculate pan direction in world space
                Vector3 right = Vector3.Cross(_cameraUp, Vector3.Normalize(_cameraPosition - _cameraTarget));
                Vector3 up = Vector3.Up;
                
                // Apply panning to camera target
                _cameraTarget -= right * delta.X * _panSpeed;
                _cameraTarget += up * delta.Y * _panSpeed;
                
                _lastMousePosition = currentMousePosition;
            }
        }
        else
        {
            _isPanning = false;
        }

        // Handle mouse wheel zoom
        if (mouseState.ScrollWheelValue != previousMouseState.ScrollWheelValue)
        {
            float zoomDelta = (mouseState.ScrollWheelValue - previousMouseState.ScrollWheelValue) * 0.01f;
            _cameraDistance = MathHelper.Clamp(_cameraDistance - zoomDelta, 5f, 100f);
        }

        // Handle input
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape))
            Exit();

        // Reset simulation (Ctrl + R)
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R) && 
            keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !previousKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R))
        {
            SpawnSpheres();
            System.Console.WriteLine("Simulation reset");
        }

        // Add one E sphere (Ctrl + 1)
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1) && 
            keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !previousKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D1))
        {
            if (_numESpheres < MAX_E_PARTICLES)
            {
                AddESphere();
                _numESpheres++;
                System.Console.WriteLine($"Added E particle. Total: {_numESpheres}/{MAX_E_PARTICLES}");
            }
        }

        // Remove one random E sphere (Ctrl + 2)
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2) && 
            keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !previousKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D2))
        {
            if (_eSpheres.Count > 0)
            {
                int randomIndex = _random.Next(_eSpheres.Count);
                _eSpheres.RemoveAt(randomIndex);
                _numESpheres--;
                System.Console.WriteLine($"Removed random E sphere. Total: {_numESpheres}");
            }
        }

        // Add one MP sphere (Ctrl + 3)
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3) && 
            keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !previousKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D3))
        {
            if (_numMPSpheres < MAX_MP_PARTICLES)
            {
                Vector3 position = new Vector3(
                    (float)(_random.NextDouble() * 40 - 20),
                    (float)(_random.NextDouble() * 40 - 20),
                    (float)(_random.NextDouble() * 40 - 20)
                );
                _mpSpheres.Add(new MPSphere(position, _attractionStrength, _mpSphereSize));
                _numMPSpheres++;
                System.Console.WriteLine($"Added MP particle. Total: {_numMPSpheres}/{MAX_MP_PARTICLES}");
            }
        }

        // Remove one MP sphere (Ctrl + 4)
        if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4) && 
            keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) &&
            !previousKeyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D4))
        {
            if (_mpSpheres.Count > 0)
            {
                int randomIndex = _random.Next(_mpSpheres.Count);
                _mpSpheres.RemoveAt(randomIndex);
                _numMPSpheres--;
                System.Console.WriteLine($"Removed random MP sphere. Total: {_numMPSpheres}");
            }
        }

        // Store the current keyboard and mouse states for the next frame
        _previousKeyboardState = keyboardState;
        _previousMouseState = mouseState;

        // Update camera position
        _cameraRotation += 0.001f;
        _cameraPosition = new Vector3(
            (float)Math.Cos(_cameraRotation) * _cameraDistance,
            20,
            (float)Math.Sin(_cameraRotation) * _cameraDistance
        );
    }
} 