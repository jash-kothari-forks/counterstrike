﻿using System;
using System.Collections.Generic;
using System.Linq;
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
using CSL.LevelEditor.Properties;
using System.IO;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Windows.Ink;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Windows.Markup;
using CSL.Common;
namespace CSL.LevelEditor
{
    /// <summary>
    /// Interaction logic for WindowEditor         
    /// </summary>
    public partial class WindowEditor : Window
    {
        public WindowEditor()
        {
            String gamePath = System.IO.Path.GetFullPath("../../../");
            Directory.SetCurrentDirectory(gamePath);
            InitializeComponent();
            Loaded += new RoutedEventHandler(WindowLoaded);
        }
        private XmlSerializer _xmlSerializer;
        private double _Scale = 1;
        private InkCanvas _currentInkCanvas;
        //TODO: make dynamical
        private String _filePath4MapDescriptor = @"C:\Source\cs\trunk\CounterStrikeLive\CounterStrikeLive\Content\map.xml";
        private MapDatabase _mapDatabase = new MapDatabase();
        private CustomStroke _Stroke;


        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            SelectCanvas(0);

            //initializations
            _xmlSerializer = new XmlSerializer(typeof(MapDatabase));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, this.SaveFile));
            this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, this.OpenFile));
            _currentInkCanvas.StrokeCollected += new InkCanvasStrokeCollectedEventHandler(InkCanvas_StrokeCollected);
            KeyDown += new KeyEventHandler(InkCanvasKeyDown);
            KeyUp += new KeyEventHandler(WindowEditor_KeyUp);
            _CanvasList.MouseDown += new MouseButtonEventHandler(InkCanvas_MouseDown);
            _CanvasList.MouseMove += new MouseEventHandler(_InkCanvas_MouseMove);
            foreach (InkCanvas inkCanvas in _CanvasList.Children)
            {
                inkCanvas.SelectionChanged += new EventHandler(InkCanvas1SelectionChanged);
            }

            OpenFile(_filePath4MapDescriptor);
            SetMode(InkCanvasEditingMode.Select, CustomMode.select);
        }



        public void SelectCanvas(int canvasIndex)
        {
            InkCanvas oldInkCanvas = _currentInkCanvas;
            _currentInkCanvas = (InkCanvas)_CanvasList.Children[canvasIndex];

            //copy images & strokes
            if (oldInkCanvas != null)
            {
                foreach (Image uIElement in oldInkCanvas.GetSelectedElements().OfType<Image>())
                {
                    oldInkCanvas.Children.Remove(uIElement);
                    _currentInkCanvas.Children.Add(uIElement);
                }
                foreach (Stroke stroke in oldInkCanvas.GetSelectedStrokes())
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift)) _currentInkCanvas.Strokes.Add(stroke.Clone());
                    if (Keyboard.IsKeyDown(Key.LeftAlt))
                    {
                        oldInkCanvas.Strokes.Remove(stroke);
                        _currentInkCanvas.Strokes.Add(stroke);
                    }
                }
            }

            //hide all canvases
            foreach (InkCanvas inkCanvas in _CanvasList.Children)
            {
                inkCanvas.Opacity = .2;
                inkCanvas.IsHitTestVisible = false;
            }

            _currentInkCanvas.IsHitTestVisible = true;
            _currentInkCanvas.Opacity = 1;
            //SetMode(InkCanvasEditingMode.Select, CustomMode.select);
            _currentInkCanvas.Select(new StrokeCollection());
            if (null != oldInkCanvas)
                oldInkCanvas.Select(new StrokeCollection());
        }

        //Loaddata
        private void OpenFile(string filePath4MapDescriptor)
        {
            try
            {
                if (!File.Exists(filePath4MapDescriptor))
                {
                    InfoMessageBox.Show("File not exists " + filePath4MapDescriptor);
                    return;
                }
                byte[] buffer = File.ReadAllBytes(filePath4MapDescriptor);
                MemoryStream memoryStream = new MemoryStream(buffer);
                _mapDatabase = (MapDatabase)_xmlSerializer.Deserialize(memoryStream);

                _currentInkCanvas.Strokes.Clear();

                InkCanvas.SetLeft(_CStartPos, _mapDatabase._CStartPos.X);
                InkCanvas.SetTop(_CStartPos, _mapDatabase._CStartPos.Y);
                InkCanvas.SetLeft(_TStartPos, _mapDatabase._TStartPos.X);
                InkCanvas.SetTop(_TStartPos, _mapDatabase._TStartPos.Y);

                for (int i = 0; i < _mapDatabase._Layers.Count; i++)
                {
                    MapDatabase.Layer layer = _mapDatabase._Layers[i];
                    InkCanvas inkCanvas = (InkCanvas)_CanvasList.Children[i];
                    foreach (MapDatabase.Image image in layer._Images)
                    {
                        Image img = new Image();
                        if (!File.Exists(image.Path))
                            throw new FileNotFoundException(image.Path);

                        BitmapImage _BitmapImage = new BitmapImage(new Uri(image.Path, UriKind.Relative));
                        double a = _BitmapImage.Width;
                        img.Source = _BitmapImage;

                        img.Width = image.Width;
                        img.Height = image.Height;
                        InkCanvas.SetLeft(img, image.X);
                        InkCanvas.SetTop(img, image.Y);
                        inkCanvas.Children.Add(img);
                    }

                    foreach (MapDatabase.Polygon polygon in layer._Polygons)
                    {
                        StylusPointCollection stylusPointCollection = new StylusPointCollection();
                        foreach (Point point in polygon._Points)
                        {
                            StylusPoint _StylusPoint = new StylusPoint(point.X, point.Y);
                            stylusPointCollection.Add(_StylusPoint);
                        }

                        CustomStroke _Stroke = new CustomStroke(stylusPointCollection);
                        _Stroke.DrawingAttributes.Color = polygon._Color;
                        inkCanvas.Strokes.Add(_Stroke);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessageBox.Show(ex);
            }
        }

        Key oldkey;
        void InkCanvasKeyDown(object sender, KeyEventArgs e)
        {
            oldkey = e.Key;
            for (int i = 0; i < 5; i++)
                if (Keyboard.IsKeyDown(Key.D1 + i))
                    SelectCanvas(i);
            //D = (int)e.Key - 83;
            //if (D >= 0 && D <= 5)
            //    SelectCanvas(D);

            if (Keyboard.IsKeyDown(Key.LeftCtrl) && Keyboard.IsKeyDown(Key.Z))
            {
                if (_Stroke != null)
                {
                    if (_curentPoint > 0)
                    {
                        _Stroke.StylusPoints.RemoveAt(_curentPoint);
                        _curentPoint--;
                    }
                    else
                    {
                        _currentInkCanvas.Strokes.Remove(_Stroke);
                        _Stroke = null;
                    }
                }
            }

            if (e.Key == Key.Q)
            {
                SetMode(InkCanvasEditingMode.Select, CustomMode.select);
            }
            if (e.Key == Key.W)
            {
                SetMode(InkCanvasEditingMode.None, CustomMode.polygon);
            }
            if (e.Key == Key.E)
            {
                SetMode(InkCanvasEditingMode.EraseByPoint, CustomMode.erase);
            }

            if (e.Key == Key.Add || e.Key == Key.Subtract)
            {
                double _ScaleFactor = e.Key == Key.Add ? 1.2 : .8;
                _Scale *= _ScaleFactor;
                _ScaleText.Text = _Scale.ToString();
                foreach (InkCanvas _InkCanvas in _CanvasList.Children)
                {
                    foreach (Stroke _Stroke in _InkCanvas.Strokes)
                        for (int i = 0; i < _Stroke.StylusPoints.Count; i++)
                        {
                            StylusPoint _StylusPoint = _Stroke.StylusPoints[i];
                            _Stroke.StylusPoints[i] = new StylusPoint(_StylusPoint.X * _ScaleFactor, _StylusPoint.Y * _ScaleFactor);
                        }
                    foreach (FrameworkElement _Image in _InkCanvas.Children)
                    {
                        InkCanvas.SetLeft(_Image, InkCanvas.GetLeft(_Image) * _ScaleFactor);
                        InkCanvas.SetTop(_Image, InkCanvas.GetTop(_Image) * _ScaleFactor);
                        _Image.Width = _Image.ActualWidth * _ScaleFactor;
                        _Image.Height = _Image.ActualHeight * _ScaleFactor;
                    }
                }
            }
            if (e.Key == Key.C)
            {
                SelectColor();
            }
            if (e.Key == Key.PageUp)
            {
                StrokeCollection _StrokeCollection = _currentInkCanvas.GetSelectedStrokes();
                foreach (Stroke _Stroke in _StrokeCollection)
                {
                    _currentInkCanvas.Strokes.Remove(_Stroke);
                    _currentInkCanvas.Strokes.Add(_Stroke);
                }
            }
            if (e.Key == Key.PageDown)
            {
                StrokeCollection _StrokeCollection = _currentInkCanvas.GetSelectedStrokes();
                foreach (Stroke _Stroke in _StrokeCollection)
                {
                    _currentInkCanvas.Strokes.Remove(_Stroke);
                    _currentInkCanvas.Strokes.Insert(0, _Stroke);
                }
            }
            if (Keyboard.IsKeyDown(Key.C) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                _currentInkCanvas.CopySelection();
            }
            //if (Keyboard.IsKeyDown(Key.X) && Keyboard.IsKeyDown(Key.LeftCtrl))
            //{
            //    _InkCanvas.CutSelection();
            //}
            if (Keyboard.IsKeyDown(Key.V) && Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                _currentInkCanvas.Paste();
            }
            if (e.Key == Key.B)
            {
                if (_PolygonsCanvas.Children.Count > 0)
                    _PolygonsCanvas.Children.Clear();
                else
                    foreach (InkCanvas _InkCanvas1 in _CanvasList.Children)
                        foreach (Stroke _Stroke in _InkCanvas1.Strokes)
                        {
                            if (_Stroke.StylusPoints.Last() == _Stroke.StylusPoints.First())
                            {
                                Polygon _Polygon = new Polygon();
                                foreach (StylusPoint _Point in _Stroke.StylusPoints)
                                {
                                    _Polygon.Points.Add(new Point(_Point.X, _Point.Y));
                                }
                                _Polygon.Fill = new SolidColorBrush(_Stroke.DrawingAttributes.Color);
                                _PolygonsCanvas.Children.Add(_Polygon);
                            }
                        }
            }
        }
        public enum CustomMode
        {
            select, ink, polygon, erase
        }
        public CustomMode _CurCustomMode;

        private void SetMode(InkCanvasEditingMode mode, CustomMode customMode)
        {
            SetStroke();
            _currentInkCanvas.EditingMode = mode;
            _CurCustomMode = customMode;
        }


        private void SetStroke()
        {
            if (_Stroke != null)
            {
                InkCanvas_StrokeCollected(_currentInkCanvas, new InkCanvasStrokeCollectedEventArgs(_Stroke));
                _Stroke = null;
            }
        }

        public double Distance(StylusPoint a, StylusPoint b)
        {
            return Distance(Convert(a), Convert(b));
        }

        public double Distance(Point a, Point b)
        {
            Vector c = Point.Subtract(a, b);
            return c.Length;
        }

        public static Point Convert(StylusPoint v)
        {
            return new Point(v.X, v.Y);
        }
        void InkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            double dist = Distance(e.Stroke.StylusPoints.First(), e.Stroke.StylusPoints.Last());
            if (dist < 5)
            {
                e.Stroke.StylusPoints[e.Stroke.StylusPoints.Count - 1] = e.Stroke.StylusPoints.First();
            }
        }

        private List<MapDatabase.Layer> SaveLayers()
        {
            List<MapDatabase.Layer> _Layers = new List<MapDatabase.Layer>();
            foreach (InkCanvas _InkCanvas in _CanvasList.Children)
            {
                MapDatabase.Layer _Layer = new MapDatabase.Layer();
                foreach (Image _Image in _InkCanvas.Children.OfType<Image>())
                {
                    _Layer._Images.Add(new MapDatabase.Image
                    {
                        Path = ((BitmapImage)_Image.Source).UriSource.OriginalString,
                        X = InkCanvas.GetLeft(_Image),
                        Y = InkCanvas.GetTop(_Image),
                        Width = _Image.ActualWidth,
                        Height = _Image.ActualHeight
                    });
                }

                List<MapDatabase.Polygon> _Polygons = _Layer._Polygons;
                foreach (CustomStroke _Stroke in _InkCanvas.Strokes)
                {
                    List<Point> _Points = new List<Point>();
                    foreach (StylusPoint _StylusPoint in _Stroke.StylusPoints)
                    {
                        Point _Point = new Point();
                        _Point.X = (int)_StylusPoint.X;
                        _Point.Y = (int)_StylusPoint.Y;
                        _Points.Add(_Point);
                    }
                    MapDatabase.Polygon _Polygon = new MapDatabase.Polygon();
                    _Polygon._Color = _Stroke.DrawingAttributes.Color;
                    _Polygon._Points = _Points;
                    _Polygons.Add(_Polygon);
                }
                _Layers.Add(_Layer);
            }
            return _Layers;
        }


        public void SaveFile(object sender, RoutedEventArgs e)
        {
            _mapDatabase._Layers = SaveLayers();

            _mapDatabase._CStartPos.X = InkCanvas.GetLeft(_CStartPos);
            _mapDatabase._CStartPos.Y = InkCanvas.GetTop(_CStartPos);
            _mapDatabase._TStartPos.X = InkCanvas.GetLeft(_TStartPos);
            _mapDatabase._TStartPos.Y = InkCanvas.GetTop(_TStartPos);

            MemoryStream _MemoryStream = new MemoryStream();
            _xmlSerializer.Serialize(_MemoryStream, _mapDatabase);

            byte[] _Buffer = _MemoryStream.ToArray();
            File.WriteAllBytes(_filePath4MapDescriptor, _Buffer);
        }
        public const string _Filter = "map (*.xml)|*.xml";

        void _InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point _pos = e.GetPosition(_currentInkCanvas);
            if (e.RightButton == MouseButtonState.Pressed && _Stroke != null)
            {
                _Stroke.StylusPoints[_curentPoint] = new StylusPoint(_pos.X, _pos.Y);
            }
            
            String text = ((int)_pos.X) + ":" + ((int)_pos.Y);
            _TextBlock.Text = text;
        }

        public void OpenFile(object sender, RoutedEventArgs e)
        {
        }




        StrokeCollection _oldStrokeCollection;
        void InkCanvas1SelectionChanged(object sender, EventArgs e)
        {
            InkCanvas _InkCanvas = (InkCanvas)sender;
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (_oldStrokeCollection != null)
                {
                    foreach (Stroke _Stroke in _InkCanvas.GetSelectedStrokes())
                        if (!_oldStrokeCollection.Contains(_Stroke))
                            _oldStrokeCollection.Add(_Stroke);
                    _InkCanvas.Select(_oldStrokeCollection);
                }
            }
            _oldStrokeCollection = _InkCanvas.GetSelectedStrokes();

        }

        void WindowEditor_KeyUp(object sender, KeyEventArgs e)
        {
        }


        void SelectColor()
        {
            StrokeCollection _StrokeCollection = _currentInkCanvas.GetSelectedStrokes();
            if (_StrokeCollection.Count > 0)
            {
                System.Windows.Forms.ColorDialog _ColorDialog = new System.Windows.Forms.ColorDialog();
                if (_ColorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    foreach (Stroke _Stroke in _StrokeCollection)
                    {

                        Color _Color = Color.FromArgb(255, _ColorDialog.Color.R, _ColorDialog.Color.G, _ColorDialog.Color.B);

                        _Stroke.DrawingAttributes.Color = _Color;
                        //StreamGeometry _Geometry = _Stroke.GetGeometry();

                    }

            }
        }
        void MediaPanel_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.None;

            string[] fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            foreach (string fileName in fileNames)
            {
                if (Regex.IsMatch(System.IO.Path.GetExtension(fileName), "jpg|png|bmp|gif", RegexOptions.IgnoreCase))
                    e.Effects = DragDropEffects.Copy;
            }

            // Mark the event as handled, so control's native DragOver handler is not called.
            e.Handled = true;
        }
        public static string RelativePath(string absolutePath, string relativeTo)
        {
            string[] absoluteDirectories = absolutePath.Split('\\');
            string[] relativeDirectories = relativeTo.Split('\\');

            //Get the shortest of the two paths
            int length = absoluteDirectories.Length < relativeDirectories.Length ? absoluteDirectories.Length : relativeDirectories.Length;

            //Use to determine where in the loop we exited
            int lastCommonRoot = -1;
            int index;

            //Find common root
            for (index = 0; index < length; index++)
                if (absoluteDirectories[index] == relativeDirectories[index])
                    lastCommonRoot = index;
                else
                    break;

            //If we didn't find a common prefix then throw
            if (lastCommonRoot == -1)
                throw new ArgumentException("Paths do not have a common base");

            //Build up the relative path
            StringBuilder relativePath = new StringBuilder();

            //Add on the ..
            for (index = lastCommonRoot + 1; index < absoluteDirectories.Length; index++)
                if (absoluteDirectories[index].Length > 0)
                    relativePath.Append("..\\");

            //Add on the folders
            for (index = lastCommonRoot + 1; index < relativeDirectories.Length - 1; index++)
                relativePath.Append(relativeDirectories[index] + "\\");
            relativePath.Append(relativeDirectories[relativeDirectories.Length - 1]);

            return relativePath.ToString();
        }

        void MediaPanel_Drop(object sender, DragEventArgs e)
        {
            string[] fileNames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

            foreach (string fileName in fileNames)
            {
                string _Ext = System.IO.Path.GetExtension(fileName);

                // Handles image files
                if (Regex.IsMatch(_Ext, "jpg|png|bmp|gif", RegexOptions.IgnoreCase))
                {
                    Image _Image = new Image();
                    _Image.Stretch = Stretch.Fill;

                    BitmapImage _BitmapImage = new BitmapImage(new Uri(RelativePath(Environment.CurrentDirectory, fileName), UriKind.Relative));
                    double a=_BitmapImage.Width;
                    _Image.Source = _BitmapImage;
                    //Debugger.Break();
                    Point _Mouse = Mouse.GetPosition(_currentInkCanvas);
                    InkCanvas.SetTop(_Image, _Mouse.X);
                    InkCanvas.SetLeft(_Image, _Mouse.Y);
                    _currentInkCanvas.Children.Add(_Image);
                }
                //TODO: handle video files
            }

            // Mark the event as handled, so the control's native Drop handler is not called.
            e.Handled = true;
        }

        public int _curentPoint;        

        void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Point _pos = e.GetPosition(_currentInkCanvas);
            if (e.RightButton == MouseButtonState.Pressed)
            {
                SetStroke();
                if (_CurCustomMode == CustomMode.polygon)
                {
                    foreach (InkCanvas _InkCanvas1 in _CanvasList.Children)
                        foreach (CustomStroke _Stroke1 in _InkCanvas1.Strokes)
                            for (int i = 0; i < _Stroke1.StylusPoints.Count; i++)
                            {
                                StylusPoint _StylusPoint = _Stroke1.StylusPoints[i];
                                double l = ((Vector)(Convert(_StylusPoint) - _pos)).Length;
                                if (l < 10)
                                {
                                    _curentPoint = i;
                                    _Stroke = _Stroke1;
                                }
                            }
                }                
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_CurCustomMode == CustomMode.polygon)
                {
                    
                    //if (Keyboard.IsKeyDown(Key.LeftCtrl))
                    {
                        foreach (InkCanvas _InkCanvas1 in _CanvasList.Children)
                            foreach (CustomStroke _Stroke1 in _InkCanvas1.Strokes)
                                for (int i = 0; i < _Stroke1.StylusPoints.Count; i++)
                                {
                                    StylusPoint _StylusPoint = _Stroke1.StylusPoints[i];
                                    double l = ((Vector)(Convert(_StylusPoint) - _pos)).Length;
                                    if (l < 5)
                                    {
                                        _pos = Convert(_StylusPoint);
                                    }
                                }
                    }

                    if (_Stroke == null)
                    {
                        StylusPointCollection _StylusPointCollection = new StylusPointCollection();
                        Point _Point = _pos;
                        _StylusPointCollection.Add(new StylusPoint(_Point.X, _Point.Y));
                        _Stroke = new CustomStroke(_StylusPointCollection);
                        _currentInkCanvas.Strokes.Add(_Stroke);

                        _curentPoint = 0;

                    }
                    else
                    {
                        StylusPoint _StylusPoint = new StylusPoint(_pos.X, _pos.Y);
                        _curentPoint++;
                        _Stroke.StylusPoints.Insert(_curentPoint, _StylusPoint);
                    }
                }
            }            
        }

        private void NewPoint(Point _pos)
        {
            //foreach (InkCanvas _InkCanvas1 in _CanvasList.Children)
            //    foreach (CustomStroke _Stroke1 in _InkCanvas1.Strokes)
            //        for (int i = 0; i < _Stroke1.StylusPoints.Count; i++)
            //        {                        
            //            StylusPoint _StylusPoint = _Stroke1.StylusPoints[i];
            //            double l = ((Vector)(Convert(_StylusPoint)-_pos)).Length;
            //            if (l < 3)
            //            {
            //                _Stroke = _Stroke1;
            //                _curentPoint = i;
            //                return;
            //            }
            //        }


        }



        public class CustomInkCanvas : InkCanvas
        {

        }
        class CustomStroke : Stroke
        {
            public CustomStroke(StylusPointCollection _StylusPointCollection)
                : base(_StylusPointCollection)
            {
            }
            //public string _ImagePath;
        }


    }
}
