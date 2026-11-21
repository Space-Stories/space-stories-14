using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Stories.AssemblyBench;

public sealed partial class StoriesAssemblyBenchSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AssemblyBenchComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AssemblyBenchComponent, AssemblyBenchDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<AssemblyBenchComponent, ExaminedEvent>(OnExamined);

        Subs.BuiEvents<AssemblyBenchComponent>(AssemblyBenchUiKey.Key,
            subs => subs.Event<AssemblyBenchApplyMessage>(OnApplyMessage));
    }

    public string GetRequirement(AssemblyBenchStep step)
    {
        var name = _proto.Index(step.Icon).Name;

        return step.Amount > 1
            ? Loc.GetString("stories-assembly-bench-amount", ("item", name), ("amount", step.Amount))
            : name;
    }

    public bool TryApply(Entity<AssemblyBenchComponent> ent, EntityUid user, EntityUid? used)
    {
        if (ent.Comp.WorkEnd > _timing.CurTime)
            return false;

        if (ent.Comp.Step >= ent.Comp.Steps.Count || used is not { } item || TerminatingOrDeleted(item))
            return false;

        var step = ent.Comp.Steps[ent.Comp.Step];

        if (step.Tool is { } quality)
        {
            if (!_tool.HasQuality(item, quality))
                return false;

            StartWork(ent, user, item, step, quality);
            return true;
        }

        if (step.Stack is { } stackType)
        {
            if (step.Amount <= 0
                || !TryComp<StackComponent>(item, out var stack)
                || stack.StackTypeId != stackType
                || !_stack.TryUse((item, stack), step.Amount))
                return false;
        }
        else
        {
            if (!_whitelist.IsWhitelistPass(step.Whitelist, item))
                return false;

            PredictedQueueDel(item);
        }

        Advance(ent, user);
        return true;
    }

    private void StartWork(Entity<AssemblyBenchComponent> ent, EntityUid user, EntityUid item, AssemblyBenchStep step, ProtoId<ToolQualityPrototype> quality)
    {
        var started = _tool.UseTool(item,
            user,
            ent,
            TimeSpan.FromSeconds(step.Delay),
            new[] { quality },
            new AssemblyBenchDoAfterEvent(ent.Comp.Step),
            out var id,
            step.Fuel);

        if (!started || id == null)
            return;

        var speed = CompOrNull<ToolComponent>(item)?.SpeedModifier ?? 1f;
        ent.Comp.WorkDuration = step.Delay / speed;
        ent.Comp.WorkEnd = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.WorkDuration);
        Dirty(ent);
        UpdateUi(ent);
    }

    private void OnInteractUsing(Entity<AssemblyBenchComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryApply(ent, args.User, args.Used);
    }

    private void OnApplyMessage(Entity<AssemblyBenchComponent> ent, ref AssemblyBenchApplyMessage args)
    {
        var actor = args.Actor;
        if (args.Step < 0
            || args.Step != ent.Comp.Step
            || ent.Comp.Step >= ent.Comp.Steps.Count
            || TerminatingOrDeleted(actor)
            || ent.Comp.WorkEnd > _timing.CurTime
            || !_actionBlocker.CanInteract(actor, ent)
            || !_interaction.InRangeUnobstructed(actor, ent.Owner, popup: true))
            return;

        if (TryApply(ent, args.Actor, _hands.GetActiveItem(args.Actor)))
            return;

        var need = GetRequirement(ent.Comp.Steps[ent.Comp.Step]);
        _popup.PopupEntity(Loc.GetString("stories-assembly-bench-need", ("item", need)), ent, args.Actor);
    }

    private void OnDoAfter(Entity<AssemblyBenchComponent> ent, ref AssemblyBenchDoAfterEvent args)
    {
        if (args.Handled || args.Step != ent.Comp.Step)
            return;

        if (args.Cancelled)
        {
            ent.Comp.WorkEnd = TimeSpan.Zero;
            Dirty(ent);
            UpdateUi(ent);
            return;
        }

        if (TerminatingOrDeleted(args.User) || args.Used is not { } tool || TerminatingOrDeleted(tool))
            return;

        args.Handled = true;
        Advance(ent, args.User);
    }

    private void OnExamined(Entity<AssemblyBenchComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange || ent.Comp.Step >= ent.Comp.Steps.Count)
            return;

        var step = ent.Comp.Steps[ent.Comp.Step];
        args.PushMarkup(Loc.GetString("stories-assembly-bench-examine",
            ("step", Loc.GetString(step.Name)),
            ("item", GetRequirement(step))));
    }

    private void Advance(Entity<AssemblyBenchComponent> ent, EntityUid user)
    {
        ent.Comp.Step++;
        ent.Comp.WorkEnd = TimeSpan.Zero;

        var finished = ent.Comp.Step >= ent.Comp.Steps.Count;
        if (finished)
        {
            ent.Comp.Step = 0;
            ent.Comp.Completed++;
        }

        Dirty(ent);
        _audio.PlayPredicted(finished ? ent.Comp.FinishSound : ent.Comp.InsertSound, ent, user);
        UpdateUi(ent);

        if (!finished)
            return;

        _popup.PopupEntity(Loc.GetString("stories-assembly-bench-finished"), ent);

        if (_net.IsClient)
            return;

        Spawn(ent.Comp.Result, Transform(ent).Coordinates);
    }

    private void UpdateUi(Entity<AssemblyBenchComponent> ent)
    {
        if (_ui.TryGetOpenUi(ent.Owner, AssemblyBenchUiKey.Key, out var bui))
            bui.Update();
    }
}
