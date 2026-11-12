using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace LukkyBoost.App.Views.Controls;

/// <summary>
/// Control vectorial nativo de WPF ultra-ligero (Sparkline / Windows 11 Task Manager style)
/// basado en Canvas + Polyline de 1.5 px + Polygon de área sin sobrecarga de SkiaSharp.
/// </summary>
public sealed class MicroGraphControl : Border
{
    private const int MaxPoints = 30;

    private readonly Canvas _canvas;
    private readonly Line _gridLineTop;
    private readonly Line _gridLineMid;
    private readonly Line _gridLineBottom;
    private readonly Polygon _areaPolygon;
    private readonly Polyline _sparkPolyline;

    private INotifyCollectionChanged? _subscribedCollection;

    public static readonly DependencyProperty ValuesProperty =
        DependencyProperty.Register(
            nameof(Values),
            typeof(IEnumerable<double>),
            typeof(MicroGraphControl),
            new PropertyMetadata(null, OnValuesPropertyChanged));

    public static readonly DependencyProperty LineBrushProperty =
        DependencyProperty.Register(
            nameof(LineBrush),
            typeof(Brush),
            typeof(MicroGraphControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8)), OnVisualPropertyChanged));

    public static readonly DependencyProperty FillBrushProperty =
        DependencyProperty.Register(
            nameof(FillBrush),
            typeof(Brush),
            typeof(MicroGraphControl),
            new PropertyMetadata(new SolidColorBrush(Color.FromArgb(38, 0x08, 0x2F, 0x49)), OnVisualPropertyChanged));

    public static readonly DependencyProperty MaxScaleProperty =
        DependencyProperty.Register(
            nameof(MaxScale),
            typeof(double),
            typeof(MicroGraphControl),
            new PropertyMetadata(100.0, OnVisualPropertyChanged));

    public static readonly DependencyProperty AutoScaleProperty =
        DependencyProperty.Register(
            nameof(AutoScale),
            typeof(bool),
            typeof(MicroGraphControl),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    public IEnumerable<double>? Values
    {
        get => (IEnumerable<double>?)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public Brush FillBrush
    {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public double MaxScale
    {
        get => (double)GetValue(MaxScaleProperty);
        set => SetValue(MaxScaleProperty, value);
    }

    public bool AutoScale
    {
        get => (bool)GetValue(AutoScaleProperty);
        set => SetValue(AutoScaleProperty, value);
    }

    public MicroGraphControl()
    {
        Background = new SolidColorBrush(Color.FromRgb(0x0A, 0x0F, 0x1D));
        BorderBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x3D));
        BorderThickness = new Thickness(1);
        CornerRadius = new CornerRadius(8);
        ClipToBounds = true;
        SnapsToDevicePixels = true;

        _canvas = new Canvas
        {
            ClipToBounds = true,
            SnapsToDevicePixels = true
        };

        var gridBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x3D));
        gridBrush.Freeze();

        _gridLineTop = CreateGridLine(gridBrush);
        _gridLineMid = CreateGridLine(gridBrush);
        _gridLineBottom = CreateGridLine(gridBrush);

        _areaPolygon = new Polygon
        {
            Fill = FillBrush,
            StrokeThickness = 0,
            IsHitTestVisible = false
        };

        _sparkPolyline = new Polyline
        {
            Stroke = LineBrush,
            StrokeThickness = 1.5,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            IsHitTestVisible = false
        };

        _canvas.Children.Add(_gridLineTop);
        _canvas.Children.Add(_gridLineMid);
        _canvas.Children.Add(_gridLineBottom);
        _canvas.Children.Add(_areaPolygon);
        _canvas.Children.Add(_sparkPolyline);

        Child = _canvas;

        SizeChanged += (_, _) => RedrawGraph();
        Unloaded += OnControlUnloaded;
    }

    private static Line CreateGridLine(Brush stroke) => new()
    {
        Stroke = stroke,
        StrokeThickness = 1,
        SnapsToDevicePixels = true,
        IsHitTestVisible = false
    };

    private static void OnValuesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MicroGraphControl ctrl)
        {
            ctrl.AttachCollection(e.NewValue as IEnumerable<double>);
            ctrl.RedrawGraph();
        }
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MicroGraphControl ctrl)
        {
            ctrl._sparkPolyline.Stroke = ctrl.LineBrush;
            ctrl._areaPolygon.Fill = ctrl.FillBrush;
            ctrl.RedrawGraph();
        }
    }

    private void AttachCollection(IEnumerable<double>? newValues)
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnValuesCollectionChanged;
            _subscribedCollection = null;
        }

        if (newValues is INotifyCollectionChanged notify)
        {
            _subscribedCollection = notify;
            _subscribedCollection.CollectionChanged += OnValuesCollectionChanged;
        }
    }

    private void OnValuesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RedrawGraph();
    }

    private void OnControlUnloaded(object sender, RoutedEventArgs e)
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnValuesCollectionChanged;
            _subscribedCollection = null;
        }
    }

    private void RedrawGraph()
    {
        double w = _canvas.ActualWidth;
        double h = _canvas.ActualHeight;
        if (w <= 4 || h <= 4) return;

        // 1. Cuadrícula horizontal tenue (#1F293D) al 25%, 50% y 75% de la altura
        SetHorizontalLine(_gridLineTop, w, h * 0.25);
        SetHorizontalLine(_gridLineMid, w, h * 0.50);
        SetHorizontalLine(_gridLineBottom, w, h * 0.75);

        if (Values is null)
        {
            _sparkPolyline.Points.Clear();
            _areaPolygon.Points.Clear();
            return;
        }

        var list = Values.TakeLast(MaxPoints).ToList();
        if (list.Count == 0)
        {
            _sparkPolyline.Points.Clear();
            _areaPolygon.Points.Clear();
            return;
        }

        double maxCap = MaxScale <= 0 ? 100.0 : MaxScale;
        if (AutoScale)
        {
            double peak = list.Max();
            maxCap = Math.Max(10.0, peak * 1.25);
        }

        double topPad = 4.0;
        double usableHeight = Math.Max(1.0, h - topPad - 2.0);
        int count = list.Count;
        double stepX = count > 1 ? w / (MaxPoints - 1.0) : w;
        double startOffsetX = (MaxPoints - count) * stepX;

        var linePts = new PointCollection(count);
        var areaPts = new PointCollection(count + 2);

        double firstX = startOffsetX;
        double lastX = startOffsetX;

        for (int i = 0; i < count; i++)
        {
            double x = startOffsetX + (i * stepX);
            double normalized = Math.Clamp(list[i] / maxCap, 0.0, 1.0);
            double y = topPad + usableHeight * (1.0 - normalized);

            if (i == 0) firstX = x;
            lastX = x;

            var pt = new Point(x, y);
            linePts.Add(pt);
            areaPts.Add(pt);
        }

        areaPts.Add(new Point(lastX, h));
        areaPts.Add(new Point(firstX, h));

        _sparkPolyline.Points = linePts;
        _areaPolygon.Points = areaPts;
    }

    private static void SetHorizontalLine(Line line, double width, double y)
    {
        line.X1 = 0;
        line.X2 = width;
        line.Y1 = y;
        line.Y2 = y;
    }
}
