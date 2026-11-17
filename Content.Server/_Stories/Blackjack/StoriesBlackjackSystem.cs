using System.Numerics;
using Content.Server._Stories.Economy;
using Content.Server.Chat.Systems;
using Content.Server.Inventory;
using Content.Server.Station.Systems;
using Content.Shared._Stories.Blackjack;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Destructible;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Stories.Blackjack;

public sealed partial class StoriesBlackjackSystem : EntitySystem
{
    [Dependency] private BankSystem _bank = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private ServerInventorySystem _inventory = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlackjackTableComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<BlackjackTableComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<BlackjackTableComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<BlackjackTableComponent, BoundUIClosedEvent>(OnUiClosed);

        Subs.BuiEvents<BlackjackTableComponent>(BlackjackUiKey.Lobby, subs =>
        {
            subs.Event<BlackjackSitMessage>(OnSit);
            subs.Event<BlackjackReadyMessage>(OnReady);
            subs.Event<BlackjackSpectateMessage>(OnSpectate);
            subs.Event<BlackjackCashierMessage>(OnCashier);
        });

        Subs.BuiEvents<BlackjackTableComponent>(BlackjackUiKey.Game, subs =>
        {
            subs.Event<BlackjackBetMessage>(OnBet);
            subs.Event<BlackjackActionMessage>(OnAction);
        });

        SubscribeLocalEvent<BlackjackTableComponent, BlackjackLeaveMessage>(OnLeave);
        SubscribeLocalEvent<BlackjackPlayerComponent, EntitySpokeEvent>(OnPlayerSpoke);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ActiveBlackjackTableComponent, BlackjackTableComponent>();
        while (query.MoveNext(out var uid, out var active, out var table))
        {
            var ent = new Entity<BlackjackTableComponent>(uid, table);
            if (table.Pose != BlackjackPose.Idle && now >= active.PoseEnd)
            {
                table.Pose = BlackjackPose.Idle;
                Dirty(ent);
            }

            switch (table.Phase)
            {
                case BlackjackPhase.Lobby:
                    UpdateLobby(ent, now);
                    break;
                case BlackjackPhase.Betting:
                    if (now >= table.PhaseEnd)
                        EndBetting(ent);
                    break;
                case BlackjackPhase.Dealing:
                    UpdateDealing(ent, active, now);
                    break;
                case BlackjackPhase.Turns:
                    UpdateTurns(ent, now);
                    break;
                case BlackjackPhase.Dealer:
                    UpdateDealer(ent, active, now);
                    break;
                case BlackjackPhase.Payout:
                    if (now >= table.PhaseEnd)
                        BeginRound(ent);
                    break;
            }
        }

        UpdateSlides(frameTime);
    }

    private void OnMapInit(Entity<BlackjackTableComponent> ent, ref MapInitEvent args)
    {
        var count = ent.Comp.SeatSpots.Count;
        ent.Comp.Seats = new List<BlackjackSeat>();
        for (var i = 0; i < count; i++)
        {
            ent.Comp.Seats.Add(new BlackjackSeat());
        }

        EnsureComp<BlackjackShoeComponent>(ent);
        var props = EnsureComp<BlackjackPropsComponent>(ent);
        props.BetPiles = new EntityUid?[count];
        props.StackPiles = new EntityUid?[count];
        Dirty(ent);
    }

    private void OnShutdown(Entity<BlackjackTableComponent> ent, ref ComponentShutdown args)
    {
        DeleteProps(ent);

        for (var i = 0; i < ent.Comp.Seats.Count; i++)
        {
            if (TryGetEntity(ent.Comp.Seats[i].Player, out var player))
                RemComp<BlackjackPlayerComponent>(player.Value);

            ent.Comp.Seats[i] = new BlackjackSeat();
        }
    }

    private void OnDestruction(Entity<BlackjackTableComponent> ent, ref DestructionEventArgs args)
    {
        for (var i = 0; i < ent.Comp.Seats.Count; i++)
        {
            var seat = ent.Comp.Seats[i];
            if (!seat.Occupied)
                continue;

            var amount = seat.Chips + AtStake(seat);
            foreach (var hand in seat.Hands)
            {
                hand.Bet = 0;
            }

            seat.Chips = 0;
            seat.Bet = 0;
            GiveChips(ent, null, amount, ent.Comp.SeatSpots[i]);
        }
    }

    private void OnUiClosed(Entity<BlackjackTableComponent> ent, ref BoundUIClosedEvent args)
    {
        if (TerminatingOrDeleted(ent) || !TryGetSeat(ent.Comp, args.Actor, out var index))
            return;

        if (args.UiKey is BlackjackUiKey.Lobby && ent.Comp.Phase == BlackjackPhase.Lobby)
            Unseat(ent, index);
        else if (args.UiKey is BlackjackUiKey.Game && ent.Comp.Phase != BlackjackPhase.Idle && ent.Comp.Phase != BlackjackPhase.Lobby)
            Leave(ent, index);
    }

    private void Say(EntityUid table, string key, params (string, object)[] args)
    {
        _chat.TrySendInGameICMessage(table, Loc.GetString("stories-blackjack-" + key, args), InGameICChatType.Speak, hideChat: false, ignoreActionBlocker: true);
    }

    private string DisplayName(EntityUid player)
    {
        return Identity.Name(player, EntityManager);
    }

    private bool TryGetSeat(BlackjackTableComponent comp, EntityUid player, out int index)
    {
        var net = GetNetEntity(player);
        for (var i = 0; i < comp.Seats.Count; i++)
        {
            if (comp.Seats[i].Player == net)
            {
                index = i;
                return true;
            }
        }

        index = -1;
        return false;
    }

    private void Refresh(Entity<BlackjackTableComponent> ent)
    {
        SyncPiles(ent);
        Dirty(ent);
    }

    private void GiveChips(Entity<BlackjackTableComponent> ent, EntityUid? player, int amount, Vector2 fallbackSpot)
    {
        if (amount <= 0)
            return;

        if (player is { } user && !TerminatingOrDeleted(user) && TryGive(user, ent.Comp.ChipsProto, ent.Comp.ChipStack, amount))
            return;

        var chips = SpawnAtPosition(ent.Comp.ChipsProto, new EntityCoordinates(ent, fallbackSpot));
        _stack.SetCount((chips, null), amount);
    }
}
