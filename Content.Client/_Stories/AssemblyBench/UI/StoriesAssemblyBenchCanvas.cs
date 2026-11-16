using System.Numerics;
using Content.Client.Clickable;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Input;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client._Stories.AssemblyBench.UI;

public sealed partial class StoriesAssemblyBenchCanvas : Control
{
    private const string BackgroundState = "background";
    private const string FinishState = "finish";
    private const string GhostSuffix = "-ghost";
    private const string WorkSuffix = "-work";
    private const float PulseSpeed = 4f;
    private const float InstallTime = 0.3f;
    private const float FlashTime = 0.45f;
    private const float InstallDrop = 10f;

    private static readonly TimeSpan AnimationGrace = TimeSpan.FromSeconds(1);

    [Dependency] private IClickMapManager _clickMap = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IResourceCache _cache = default!;

    private RSI? _rsi;
    private IReadOnlyList<string> _steps = Array.Empty<string>();
    private int _current = -1;
    private TimeSpan _workEnd;
    private float _workDuration;
    private int _installIndex = -1;
    private TimeSpan _installStart;
    private TimeSpan _finishStart;
    private bool _hovered;
    private Vector2? _mouse;

    public int PixelScale { get; set; } = 4;

    public event Action? OnZonePressed;

    public StoriesAssemblyBenchCanvas()
    {
        IoCManager.InjectDependencies(this);
        MouseFilter = MouseFilterMode.Stop;
        RectClipContent = true;
    }

    private bool FinishPlaying => _current == 0 && (float)(_timing.RealTime - _finishStart).TotalSeconds < (float)FinishLength().TotalSeconds;

    public bool Working => _workDuration > 0f && _workEnd > _timing.CurTime;

    public void SetState(ResPath sprite, IReadOnlyList<string> steps, int current, TimeSpan workEnd, float workDuration)
    {
        _rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / sprite).RSI;

        var now = _timing.RealTime;
        if (_current >= 0 && steps.Count > 0)
        {
            if (current == _current + 1)
            {
                if (_installIndex != _current || now - _installStart > AnimationGrace)
                {
                    _installIndex = _current;
                    _installStart = now;
                }
            }
            else if (current == 0 && _current == steps.Count - 1 && now - _finishStart > FinishLength() + AnimationGrace)
            {
                _finishStart = now;
            }
        }

        _steps = steps;
        _current = current;
        _workEnd = workEnd;
        _workDuration = workDuration;
        UpdateHover();
        InvalidateMeasure();
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        return _rsi == null ? Vector2.Zero : (Vector2)_rsi.Size * PixelScale;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_rsi == null)
            return;

        var now = _timing.RealTime;
        var seconds = (float)now.TotalSeconds;
        DrawState(handle, BackgroundState, seconds, true, Color.White, 0f);

        var sinceFinish = (float)(now - _finishStart).TotalSeconds;
        if (FinishPlaying)
        {
            DrawState(handle, FinishState, sinceFinish, false, Color.White, 0f);
            return;
        }

        var sinceInstall = (float)(now - _installStart).TotalSeconds;
        for (var i = 0; i < _current && i < _steps.Count; i++)
        {
            if (i == _installIndex && sinceInstall < InstallTime && !_rsi.TryGetState(_steps[i] + WorkSuffix, out _))
            {
                var remaining = 1f - sinceInstall / InstallTime;
                var ease = 1f - remaining * remaining * remaining;
                DrawState(handle, _steps[i], seconds, true, Color.White.WithAlpha(ease), -InstallDrop * (1f - ease));
                continue;
            }

            DrawState(handle, _steps[i], seconds, true, Color.White, 0f);
        }

        if (_installIndex >= 0 && _installIndex < _current && _installIndex < _steps.Count && sinceInstall < FlashTime)
            DrawState(handle, _steps[_installIndex] + GhostSuffix, seconds, true, Color.White.WithAlpha(1f - sinceInstall / FlashTime), 0f);

        if (_current >= _steps.Count)
            return;

        var id = _steps[_current];
        if (Working && _rsi.TryGetState(id + WorkSuffix, out var work))
        {
            var left = (float)(_workEnd - _timing.CurTime).TotalSeconds;
            var progress = Math.Clamp(1f - left / _workDuration, 0f, 1f);
            var frame = Math.Min((int)(progress * work.DelayCount), work.DelayCount - 1);
            handle.DrawTextureRect(work.GetFrame(RsiDirection.South, frame), PixelSizeBox);
            return;
        }

        var pulse = 0.5f + 0.5f * MathF.Sin(seconds * PulseSpeed);
        var alpha = _hovered ? 1f : 0.55f + 0.35f * pulse;
        DrawState(handle, id + GhostSuffix, seconds, true, Color.White.WithAlpha(alpha), 0f);
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick || !HitsZone(args.RelativePosition))
            return;

        OnZonePressed?.Invoke();
        args.Handle();
    }

    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        _mouse = args.RelativePosition;
        UpdateHover();
    }

    private void UpdateHover()
    {
        _hovered = _mouse is { } mouse && HitsZone(mouse);
        DefaultCursorShape = _hovered ? CursorShape.Hand : CursorShape.Arrow;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_mouse != null)
            UpdateHover();
    }

    protected override void MouseExited()
    {
        base.MouseExited();

        _mouse = null;
        UpdateHover();
    }

    private TimeSpan FinishLength()
    {
        return _rsi != null && _rsi.TryGetState(FinishState, out var finish)
            ? TimeSpan.FromSeconds(finish.AnimationLength)
            : TimeSpan.Zero;
    }

    private void DrawState(DrawingHandleScreen handle, string id, float time, bool loop, Color modulate, float offsetY)
    {
        if (!_rsi!.TryGetState(id, out var state))
            return;

        var frame = 0;
        if (state.DelayCount > 1 && state.AnimationLength > 0f)
        {
            var position = loop ? time % state.AnimationLength : Math.Min(time, state.AnimationLength);
            frame = Math.Min((int)(position / state.AnimationLength * state.DelayCount), state.DelayCount - 1);
        }

        var box = UIBox2.FromDimensions(new Vector2(0f, offsetY * PixelScale * UIScale), PixelSize);
        handle.DrawTextureRect(state.GetFrame(RsiDirection.South, frame), box, modulate);
    }

    private bool HitsZone(Vector2 relative)
    {
        if (_rsi == null || _current < 0 || _current >= _steps.Count || Working || FinishPlaying)
            return false;

        var pos = (Vector2i)(relative / PixelScale);
        return _clickMap.IsOccluding(_rsi, _steps[_current] + GhostSuffix, RsiDirection.South, 0, pos);
    }
}
