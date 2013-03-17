using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using AnimationAux;

namespace BetterSkinned
{
    /// <summary>
    /// This is the main class for your game
    /// </summary>
    public class SkinnedGame : Microsoft.Xna.Framework.Game
    {
        #region Fields

        /// <summary>
        /// This graphics device we are drawing on in this program
        /// </summary>
        GraphicsDeviceManager graphics;

        /// <summary>
        /// The camera we use
        /// </summary>
        private Camera camera;

        private Axes axes;
        private Axes targetMarker;

        /// <summary>
        /// The animated model we are displaying
        /// </summary>
        private AnimatedModel model = null;

        /// <summary>
        /// This model is loaded solely for the dance animation
        /// </summary>
        //private AnimatedModel dance = null;

        //int boneIndexToDrawAxes = 0;

        //Vector3 cameraLocWorldSpace;
        Vector3 targetPointWorldSpace;

        AnimationPlayer player;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        public SkinnedGame()
        {
            // XNA startup
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";


            // Some basic setup for the display window
            this.IsMouseVisible = true;
			this.Window.AllowUserResizing = true;
			this.graphics.PreferredBackBufferWidth = 1024;
			this.graphics.PreferredBackBufferHeight = 768;

            // Create a simple mouse-based camera
            camera = new Camera(graphics);
            camera.Eye = new Vector3(0.0f, 60.0f, -80.0f);
            camera.Center = new Vector3(1, 60, 0);
            //camera.Eye = new Vector3(190, 247, 387);
            //camera.Center = new Vector3(-20, 86, 159);-1.0f, 60.0f, -20.0f

            axes = new Axes(20.0f, 2.0f, 4, graphics);
            targetMarker = new Axes(5.0f, 2.0f, 4, graphics);
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            camera.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Load the model we will display
            //model = new AnimatedModel("Victoria-hat-tpose");
            model = new AnimatedModel("dude");
            model.LoadContent(Content);

            // Load the model that has an animation clip it in
            //dance = new AnimatedModel("Victoria-hat-dance");
            //dance.LoadContent(Content);

            // Obtain the clip we want to play. I'm using an absolute index, 
            // because XNA 4.0 won't allow you to have more than one animation
            // associated with a model, anyway. It would be easy to add code
            // to look up the clip by name and to index it by name in the model.
            //AnimationClip clip = dance.Clips[0];
            //AnimationClip clip = model.Clips[0];
            AnimationClip clip = model.Clips[0];

            // And play the clip
            player = model.PlayClip(clip);
            player.Looping = true;

            axes.LoadGraphicsContent(true);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }


        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

           // Aim head and eyes at the camera center
            //targetPointWorldSpace = new Vector3((float)(80.0 * Math.Sin(gameTime.TotalGameTime.TotalSeconds)), 60.0f, -20.0f);
            //targetPointWorldSpace = new Vector3(-80.0f, 60.0f, -20.0f);
            targetPointWorldSpace = camera.Eye;

           model.Update(gameTime, targetPointWorldSpace);

            camera.Update(graphics.GraphicsDevice, gameTime);
            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.LightGray);

            axes.Draw(camera.View, Matrix.Identity, camera.Projection);
            model.Draw(graphics.GraphicsDevice, camera, Matrix.Identity, axes);

            //axes.Draw(camera.View, Matrix.CreateTranslation(targetPointWorldSpace), camera.Projection);
            DrawReferenceFrames(camera);
            base.Draw(gameTime);
        }

        protected void DrawReferenceFrames(Camera camera)
        {
            // Show the world frame
/*            axes.Draw(camera.View, Matrix.Identity, camera.Projection);
            axes.Draw(camera.View, Matrix.CreateTranslation(targetPointWorldSpace), camera.Projection);

            Matrix[] boneToWorldArray = player.GetWorldTransforms();

            for (int boneIndex = 0; boneIndex < boneToWorldArray.Length; ++boneIndex)
            {
                if (boneIndex == boneIndexToDrawAxes)
                {
                    axes.Draw(camera.View, boneToWorldArray[boneIndex], camera.Projection);
                }
            }
 * */
        }
    }
}
