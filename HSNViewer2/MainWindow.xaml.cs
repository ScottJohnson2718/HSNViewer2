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

        // A yaw and pitch applied to the second viewport based on input
        private float yaw = 0f;
        private float pitch = 0f;

        private bool playing = true;

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
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            string relativePath = System.IO.Path.Combine(assemblyLocation, "../../../Content");
            string contentPath = System.IO.Path.GetFullPath(relativePath);

            // Since the build project doesn't work, use the builder
            //contentBuilder.Add("f:\\Content\\Dude.fbx", "Model", null, "ModelProcessor");

            // The Content project creates the .XNB files.  The XNA game project has a reference to it that makes it build.
            // On a successful build of the XNA game project there is a custom build step to xcopy the files from the XNA Game
            // project to a top directory.  Then we create a relative path "contentPath" from the location of the HSNViewer2.exe
            // to that top path.  This makes it so that the content pipeline creates the XNB files and the HSNViewer2 project
            // finds those files without any manual steps on your part.
            contentManager = new ContentManager(xnaControl1.Services, contentPath);

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

            float percentPlayed = player.Position / player.Duration;

            // Is this data binding?  How do I set the value?
            //PlayStopButton.
 
        }

        // Invoked when the mouse moves over the second viewport
        private void xnaControl1_MouseMove(object sender, HwndMouseEventArgs e)
        {
            // If the left or right buttons are down, we adjust the yaw and pitch of the cube
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed )
            {
                yaw = (float)(e.Position.X - e.PreviousPosition.X) * .01f;
                pitch = (float)(e.Position.Y - e.PreviousPosition.Y) * .01f;

                camera.Yaw(yaw);
                camera.Pitch(pitch);
            }
            if ( e.RightButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                float pan = (float)(e.Position.X - e.PreviousPosition.X) * .01f;
                camera.Pan(pan);
            }
        }

        private void xnaControl1_MouseWheel(object sender, HwndMouseEventArgs e)
        {
            int delta = (e.WheelDelta >> 16) / NativeMethods.WHEEL_DELTA;
            camera.Zoom(-delta * 5.0f);
        }

        // We use the left mouse button to do exclusive capture of the mouse so we can drag and drag
        // to rotate the cube without ever leaving the control
        private void xnaControl1_HwndLButtonDown(object sender, HwndMouseEventArgs e)
        {
            xnaControl1.CaptureMouse();
        }

        private void xnaControl1_HwndLButtonUp(object sender, HwndMouseEventArgs e)
        {
            xnaControl1.ReleaseMouseCapture();
        }

        private void PlayStopButton_PlayStop(object sender, RoutedEventArgs e)
        {
            playing = !playing;
            if (playing)
            {
                // todo : so much to learn
            }
        }

    }
}
