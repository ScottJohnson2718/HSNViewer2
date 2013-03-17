using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics; // for stopwatch
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
//using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using WpfHostedXna;
using Primitives3D;

using AnimationAux;
using BetterSkinned;

namespace HSNViewer2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //ContentBuilder contentBuilder;
        ContentManager contentManager;
     
        private CubePrimitive cube;

        // We use a Stopwatch to track our total time for cube animation
        private Stopwatch watch = new Stopwatch();
         private TimeSpan lastTime;

        // A yaw and pitch applied to the second viewport based on input
        //private float yaw = 0f;
        //private float pitch = 0f;

        // The color applied to the cube in the second viewport
        Color cubeColor = Color.Red;

        Camera camera;

        /// <summary>
        /// The animated model we are displaying
        /// </summary>
        private AnimatedModel model = null;

        AnimationPlayer player;

        private TimeSpan totalGameTime = new TimeSpan();
 
        public MainWindow()
        {
            InitializeComponent();

        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Got here");
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Invoked after either control has created its graphics device.
        /// </summary>
        private void loadContent(object sender, GraphicsDeviceEventArgs e)
        {
            // Start the watch now that we're going to be starting our draw loop
            watch.Start();

           // contentBuilder = new ContentBuilder();
            // Init everything that depends on a GraphicsDevice for construction
            camera = new Camera(e.GraphicsDevice);
            camera.Eye = new Vector3(0.0f, 60.0f, -80.0f);
            camera.Center = new Vector3(1, 60, 0);

            camera.Initialize();


           cube = new CubePrimitive(e.GraphicsDevice);

          // Load the model we will display
            //model = new AnimatedModel("Victoria-hat-tpose");
            model = new AnimatedModel("dude");

            // Default to the directory which contains our content files.
            //string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            //string relativePath = System.IO.Path.Combine(assemblyLocation, "../../../../Content");
            //string contentPath = System.IO.Path.GetFullPath(relativePath);

            // Since the build project doesn't work, use the builder
            //contentBuilder.Add("f:\\Content\\Dude.fbx", "Model", null, "ModelProcessor");

    // THe content project doesn't work away from the XNA Game project.  It has a content reference to the BetterSkinned
    // Content project.  I haven't found a way to get the content project to build in the context of this Solution.
    // Manually copied the results of the content pipeline to a Content directory.  A clean of the build may remove
    // it entirely.  This project is currently broken.
            contentManager = new ContentManager(xnaControl1.Services, "f:\\Content");

            Cursor = Cursors.Wait;

            // We can only use the builder when I figure out how to get it to use the custom content. Plus
            // I don't want it to build the model every time the program runs.
            // Build this new model data.
            //string buildError = contentBuilder.Build();

             // load the temporary .xnb file that we just created.
             model.LoadContent(contentManager);
 
            Cursor = Cursors.Arrow;

            // Obtain the clip we want to play. I'm using an absolute index, 
            // because XNA 4.0 won't allow you to have more than one animation
            // associated with a model, anyway. It would be easy to add code
            // to look up the clip by name and to index it by name in the model.
            AnimationClip clip = model.Clips[0];

            // And play the clip
            player = model.PlayClip(clip);
            player.Looping = true;

        }

        /// <summary>
        /// Invoked when our first control is ready to render.
        /// </summary>
        private void xnaControl1_RenderXna(object sender, GraphicsDeviceEventArgs e)
        {
            // Fake the XNA update call
            if (lastTime == null)
            {
                lastTime = new TimeSpan();
                lastTime = watch.Elapsed;
            }
            
            TimeSpan deltaTime = watch.Elapsed - lastTime;
            totalGameTime += deltaTime;
            GameTime gameTime = new GameTime(totalGameTime, deltaTime);
            lastTime = watch.Elapsed;
 
            Update(gameTime);
            camera.Update(e.GraphicsDevice, gameTime);

            // Now actually do render stuff

            e.GraphicsDevice.Clear(Color.CornflowerBlue);

            // Compute some values for the cube rotation
            float time = (float)watch.Elapsed.TotalSeconds;

            Color color = Color.Aqua;

            // Draw a cube so that it draws something while I debug the Animated Model
            cube.Draw(Matrix.Identity, camera.View, camera.Projection, color);


            model.Draw(e.GraphicsDevice, camera, Matrix.Identity);
        }

        // This is coming from the render tick which is different than actual XNA but we are
        // faking an actual XNA Game object so it will have to do.
        private void Update(GameTime deltaTime)
        {
            model.Update(deltaTime, Vector3.Zero);
 
        }
    }
}
