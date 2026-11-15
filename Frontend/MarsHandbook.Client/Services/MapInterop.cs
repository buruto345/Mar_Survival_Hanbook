using Microsoft.JSInterop;
using MarsHandbook.Client.Models;
namespace MarsHandbook.Client.Services;

public sealed class MapInterop : IAsyncDisposable
{
    readonly IJSRuntime _js;
    IJSObjectReference? _m;
    DotNetObjectReference<MapInterop>? _self;

    public event Action<List<RoutePoint>>? RouteChanged;
    public event Action<string>? PoiClicked;
    public event Action<string>? BasemapStatus;
    public event Action<double>? EvaProgress;
    public event Action? EvaDone;

    public MapInterop(IJSRuntime js) => _js = js;

    public async Task InitAsync(string elementId)
    {
        _m = await _js.InvokeAsync<IJSObjectReference>("import", "./js/mapInterop.js");
        _self = DotNetObjectReference.Create(this);
        await _m.InvokeVoidAsync("initMap", elementId, _self);
    }

    Task Call(string fn, params object?[] a) => _m is null ? Task.CompletedTask : _m.InvokeVoidAsync(fn, a).AsTask();

    public Task SetBasemap(string key) => Call("setBasemap", key);
    public Task SetPois(IEnumerable<Poi> pois) => Call("setPois", (object)pois.Select(p => new { id = p.Id, name = p.Name, lat = p.Lat, lng = p.Lng, kind = p.Kind }).ToArray());
    public Task SetDrawing(bool on) => Call("setDrawing", on);
    public Task SetRoute(IEnumerable<RoutePoint> pts) => Call("setRoute", (object)pts.Select(p => new { lat = p.Lat, lng = p.Lng }).ToArray());
    public Task ClearRoute() => Call("clearRoute");
    public Task UndoPoint() => Call("undoPoint");
    public Task FlyTo(double lat, double lng, int zoom, double dur = 1.4) => Call("flyTo", lat, lng, zoom, dur);
    public Task StartEva(int ms, bool outAndBack) => Call("startEva", ms, outAndBack);
    public Task StopEva() => Call("stopEva");
    public Task StartStars(string id) => Call("startStars", id);
    public Task SetReach(double lat, double lng, double km) => Call("setReach", lat, lng, km);
    public Task FitRoute() => Call("fitRoute");

    [JSInvokable] public void OnRouteChanged(List<RoutePoint> pts) => RouteChanged?.Invoke(pts);
    [JSInvokable] public void OnPoiClicked(string id) => PoiClicked?.Invoke(id);
    [JSInvokable] public void OnBasemapStatus(string s) => BasemapStatus?.Invoke(s);
    [JSInvokable] public void OnEvaProgress(double f) => EvaProgress?.Invoke(f);
    [JSInvokable] public void OnEvaDone() => EvaDone?.Invoke();

    public async ValueTask DisposeAsync()
    {
        try { if (_m != null) { await _m.InvokeVoidAsync("disposeMap"); await _m.DisposeAsync(); } } catch { }
        _self?.Dispose();
        RouteChanged = null; PoiClicked = null; BasemapStatus = null; EvaProgress = null; EvaDone = null;
    }
}
