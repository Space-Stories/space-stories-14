using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Stories.Heretic.Components;
using Content.Shared._Stories.Heretic.Events;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Stories.Heretic;

public sealed partial class StoriesHereticSystem
{
    private readonly List<(EntProtoId Id, StoriesHereticKnowledgeComponent Comp)> _knowledge = new();

    public IReadOnlyList<(EntProtoId Id, StoriesHereticKnowledgeComponent Comp)> AllKnowledge => _knowledge;

    public bool KnowsKnowledge(Entity<StoriesHereticComponent> ent, EntProtoId id)
    {
        return ent.Comp.Knowledge.Contains(id);
    }

    public IReadOnlyList<ProtoId<TagPrototype>> GetKnowledgeRitualTags(Entity<StoriesHereticComponent> ent)
    {
        return ent.Comp.KnowledgeRitualTags;
    }

    private void InitializeKnowledge()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        Subs.BuiEvents<StoriesHereticComponent>(StoriesHereticKnowledgeUiKey.Key,
            subs =>
            {
                subs.Event<StoriesHereticLearnMessage>(OnLearnMessage);
            });

        CacheKnowledge();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            CacheKnowledge();
    }

    private void CacheKnowledge()
    {
        _knowledge.Clear();
        foreach (var proto in ProtoMan.EnumeratePrototypes<EntityPrototype>())
        {
            if (proto.TryComp<StoriesHereticKnowledgeComponent>(out var knowledge, _factory))
                _knowledge.Add((proto.ID, knowledge));
        }
    }

    private void OnLearnMessage(Entity<StoriesHereticComponent> ent, ref StoriesHereticLearnMessage args)
    {
        TryLearn(ent, args.Knowledge);
    }

    public bool TryGetKnowledge(EntProtoId id, [NotNullWhen(true)] out StoriesHereticKnowledgeComponent? knowledge)
    {
        foreach (var (knownId, comp) in _knowledge)
        {
            if (knownId != id)
                continue;

            knowledge = comp;
            return true;
        }

        knowledge = null;
        return false;
    }

    public bool CanLearn(Entity<StoriesHereticComponent> ent, EntProtoId id, [NotNullWhen(false)] out string? reason)
    {
        reason = null;

        if (!TryGetKnowledge(id, out var knowledge))
            reason = Loc.GetString("stories-heretic-learn-unknown");
        else if (ent.Comp.Knowledge.Contains(id))
            reason = Loc.GetString("stories-heretic-learn-known");
        else if (knowledge.ChoosesPath && ent.Comp.Path != null)
            reason = Loc.GetString("stories-heretic-learn-path-chosen");
        else if (!knowledge.ChoosesPath && knowledge.Path != null && knowledge.Path != ent.Comp.Path)
            reason = Loc.GetString("stories-heretic-learn-wrong-path");
        else if (knowledge.Requires.Count > 0 && !knowledge.Requires.Any(r => ent.Comp.Knowledge.Contains(r)))
            reason = Loc.GetString("stories-heretic-learn-requires");
        else if (knowledge.Ascension && !ent.Comp.CanAscend)
            reason = Loc.GetString("stories-heretic-learn-ascension-locked");
        else if (knowledge.Ascension && !ObjectivesComplete(ent.Comp))
            reason = Loc.GetString("stories-heretic-learn-ascension-objectives");
        else if (ent.Comp.Points < knowledge.Cost)
            reason = Loc.GetString("stories-heretic-learn-no-points");

        return reason == null;
    }

    public bool TryLearn(Entity<StoriesHereticComponent> ent, EntProtoId id)
    {
        if (!CanLearn(ent, id, out var reason))
        {
            _popup.PopupEntity(reason, ent, ent);
            return false;
        }

        TryGetKnowledge(id, out var knowledge);
        ent.Comp.Points -= knowledge!.Cost;
        ent.Comp.LearnedCount++;
        Grant(ent, id, knowledge);
        return true;
    }

    private void Grant(Entity<StoriesHereticComponent> ent, EntProtoId id, StoriesHereticKnowledgeComponent knowledge)
    {
        if (!ent.Comp.Knowledge.Add(id))
            return;

        if (knowledge.ChoosesPath)
            ent.Comp.Path = knowledge.Path;

        foreach (var action in knowledge.Actions)
        {
            if (_actions.AddAction(ent, action) is { } actionId)
                ent.Comp.GrantedActions.Add(actionId);
        }

        foreach (var ritual in knowledge.Rituals)
        {
            ent.Comp.Rituals.Add(ritual);
        }

        if (knowledge.LocksAscension)
            ent.Comp.CanAscend = false;

        if (knowledge.GrantPoints > 0)
            AddPoints(ent, knowledge.GrantPoints);

        EntityManager.AddComponents(ent, knowledge.AddComponents, removeExisting: false);

        var ev = new StoriesHereticKnowledgeLearnedEvent(id);
        RaiseLocalEvent(ent, ref ev);
        Dirty(ent);
    }
}
