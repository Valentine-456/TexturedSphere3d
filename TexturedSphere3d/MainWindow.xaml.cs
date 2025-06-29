using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace TexturedSphere3d
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Sphere sphere;
        private Camera camera;
        private WriteableBitmap bitmap = null;
        public MainWindow()
        {
            InitializeComponent(); 
            Loaded += OnLoaded;
        }

        

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateScene();
        }

        private void UpdateHandler(object sender, RoutedEventArgs e)
        {
            UpdateScene();
        }

        private void LoadTexture_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg";

            if (openFileDialog.ShowDialog() == true)
            {
                var bitmapImage = new BitmapImage(new Uri(openFileDialog.FileName));
                bitmap = new WriteableBitmap(bitmapImage);
                UpdateScene();
            }
        }

        private void UpdateScene()
        {
            float distance = (float)DistanceSlider.Value;
            float rotX = (float)RotateXSlider.Value;
            float rotY = (float)RotateYSlider.Value;

            float radius = (float)RadiusSlider.Value;
            int latDiv = (int)LatDivSlider.Value;
            int lonDiv = (int)LonDivSlider.Value;

            float radX = MathF.PI * rotX / 180f;
            float radY = MathF.PI * rotY / 180f;
            Vector3 pos = new Vector3(
                distance * MathF.Cos(radX) * MathF.Sin(radY),
                distance * MathF.Sin(radX),
                distance * MathF.Cos(radX) * MathF.Cos(radY)
            );

            camera = new Camera(pos, Vector3.Zero, (float)Viewport.ActualWidth / (float)Viewport.ActualHeight);
            sphere = new Sphere(radius, latDiv, lonDiv);
            if(bitmap != null )
            {
                sphere.textureBitmap = bitmap;
            }
            sphere.Render(camera, Viewport);
        }
    }
}