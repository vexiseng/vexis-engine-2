using Avalonia;using Avalonia.Controls;using Avalonia.Input;using Avalonia.Media;
namespace Vexis.Editor.Desktop;
public sealed class WorldMapView:Control
{
 private double _zoom=1;private Vector _pan;private Point _last;private bool _drag;
 public WorldMapView(){ClipToBounds=true;PointerWheelChanged+=(_,e)=>{_zoom=Math.Clamp(_zoom*(e.Delta.Y>0?1.2:.833),.4,8);InvalidateVisual();};PointerPressed+=(_,e)=>{_drag=true;_last=e.GetPosition(this);};PointerReleased+=(_,_)=>_drag=false;PointerMoved+=(_,e)=>{if(!_drag)return;var p=e.GetPosition(this);_pan+=p-_last;_last=p;InvalidateVisual();};}
 public override void Render(DrawingContext dc){dc.FillRectangle(new SolidColorBrush(Color.Parse("#121820")),Bounds);var center=new Point(Bounds.Width/2+_pan.X,Bounds.Height/2+_pan.Y);var s=55*_zoom;
   for(int x=-8;x<=8;x++)for(int y=-6;y<=6;y++){var r=new Rect(center.X+x*s,center.Y+y*s,s,s);var h=Math.Sin(x*.7)+Math.Cos(y*.8);var c=h>.7?"#5B7045":h<-.8?"#284F66":"#6B7951";dc.FillRectangle(new SolidColorBrush(Color.Parse(c)),r);dc.DrawRectangle(null,new Pen(new SolidColorBrush(Color.Parse("#1C242B")),.5),r);}
   var water=new StreamGeometry();using(var g=water.Open()){g.BeginFigure(center+new Vector(-220*_zoom,-60*_zoom),true);g.CubicBezierTo(center+new Vector(-100*_zoom,-170*_zoom),center+new Vector(80*_zoom,-100*_zoom),center+new Vector(180*_zoom,-20*_zoom));g.CubicBezierTo(center+new Vector(130*_zoom,80*_zoom),center+new Vector(-80*_zoom,100*_zoom),center+new Vector(-220*_zoom,-60*_zoom));g.EndFigure(true);}dc.DrawGeometry(new SolidColorBrush(Color.Parse("#326D87")),new Pen(new SolidColorBrush(Color.Parse("#83B8C6")),2),water);
   dc.DrawText(new FormattedText("VAELOR WORLD MAP • generated layers preview",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,Typeface.Default,16,Brushes.White),new Point(15,15));dc.DrawText(new FormattedText("Drag to pan • Wheel to zoom • terrain / water / roads / labels / POIs",System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,Typeface.Default,12,Brushes.LightGray),new Point(15,42));}
}
