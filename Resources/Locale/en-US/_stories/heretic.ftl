stories-heretic-points-gained = You gain knowledge of the Mansus: +{ $amount }.
stories-heretic-learn-unknown = No such knowledge exists.
stories-heretic-learn-known = You already possess this knowledge.
stories-heretic-learn-path-chosen = You have already chosen your path.
stories-heretic-learn-wrong-path = This knowledge is foreign to your path.
stories-heretic-learn-requires = Learn the preceding knowledge first.
stories-heretic-learn-ascension-locked = The Feast of Owls has closed the path of Ascension.
stories-heretic-learn-feast-locked = You have already set foot on the path of Ascension.
stories-heretic-learn-ascension-objectives = Complete your objectives first.
stories-heretic-learn-no-points = Not enough knowledge of the Mansus.
stories-roles-antag-heretic-name = Heretic
stories-roles-antag-heretic-objective = Learn the forbidden knowledge of the Mansus, perform sacrifices and ascend.
stories-heretic-role-greeting = You are a Heretic. The veil of reality has thinned and you hear the whispers of the Mansus. Gather influences, perform sacrifices and ascend. Open the knowledge tree with the "Mansus knowledge" action.
stories-heretic-round-end-agent-name = heretic
stories-heretic-title = Heretics
stories-heretic-description = Some of the crew hear the call of the Mansus.
stories-heretic-round-end-count = There were { $count } heretics.
stories-heretic-round-end-entry = { $name } ({ $user }) — path: { $path }, knowledge: { $knowledge }, sacrifices: { $sacrifices }.
stories-heretic-round-end-ascended = [color=#58a65c]{ $name } ({ $user }) ascended![/color] Path: { $path }, knowledge: { $knowledge }, sacrifices: { $sacrifices }.
stories-heretic-round-end-lost = { $name } ({ $user }) — the heretic's body was lost.
stories-heretic-path-none = none
stories-heretic-path-STHereticPathAsh = Ash
stories-heretic-objective-issuer = [color=#58a65c]The Mansus[/color]
stories-heretic-objective-knowledge = Learn { $count } Mansus knowledges.
stories-heretic-objective-sacrifice = Sacrifice { $count } targets.
stories-heretic-objective-head-sacrifice = { $count ->
    [0] No head of staff sacrifice required: there are no living heads left on the station.
   *[other] Sacrifice a head of staff ({ $count }).
}

stories-heretic-rune-need-pen = You need a pen or another writing tool in your active hand.
stories-heretic-rune-bad-tile = You cannot draw a rune here.
stories-heretic-rune-too-close = Too close to another rune.
stories-heretic-ritual-success = The rune flares and the ritual is complete.
stories-heretic-ritual-nothing = The rune stays silent: no ritual fits.
stories-heretic-ritual-limit = The Mansus will not grant you more of these.
stories-heretic-ritual-missing = Missing: { $item } ×{ $amount }.
stories-heretic-tag-Knife = knife
stories-heretic-tag-Candle = candle
stories-heretic-tag-Book = book
stories-heretic-tag-GlassShard = glass shard
stories-heretic-tag-Matchstick = matchstick
stories-heretic-tag-Write = writing tool
stories-heretic-tag-Sheet = material sheet
stories-heretic-ingredient-knife = knife
stories-heretic-no-focus = You cannot cast this without a focus.
stories-heretic-codex-open = You open the Codex. Its pages whisper.
stories-heretic-codex-close = You close the Codex.
stories-heretic-ingredient-glass-sheet = glass sheet
stories-heretic-ingredient-eyes = eyes
stories-heretic-ingredient-book = book
stories-heretic-ingredient-pen = writing tool
stories-heretic-ingredient-hide = hide or leather

stories-heretic-grasp-blocked = Holy power repels the Grasp!
stories-heretic-blade-no-place = The Mansus has nowhere to take you.
stories-heretic-sacrifice-not-target = The Mansus will not accept this sacrifice.
stories-heretic-sacrifice-resisting = The victim is still resisting.
stories-heretic-sacrifice-victim = You are pulled through the veil of reality...
stories-heretic-sacrifice-returned = You return. You do not remember who did this.
stories-heretic-ingredient-poppy = poppy
stories-heretic-ingredient-clothing = clothing
stories-heretic-heart-no-targets = Your heart is silent: there are no targets.
stories-heretic-heart-other-map = { $target } is somewhere far away, beyond this place.
stories-heretic-heart-direction = { $target }: { $direction }, { $range }.
stories-heretic-range-near = near
stories-heretic-range-medium = not far
stories-heretic-range-far = far away
stories-heretic-direction-North = to the north
stories-heretic-direction-NorthEast = to the north-east
stories-heretic-direction-East = to the east
stories-heretic-direction-SouthEast = to the south-east
stories-heretic-direction-South = to the south
stories-heretic-direction-SouthWest = to the south-west
stories-heretic-direction-West = to the west
stories-heretic-direction-NorthWest = to the north-west
stories-heretic-direction-Invalid = right here

stories-heretic-pierced-examine = [color=#c0392b]Just looking at it makes your head spin.[/color]
stories-heretic-pierced-examine-heretic = You have already drunk the knowledge from this tear.
stories-heretic-pierced-touch = Something on the other side grabs you!

stories-heretic-ui-title = Mansus knowledge
stories-heretic-ui-points = ◆ { $points } knowledge { $points ->
    [one] point
   *[other] points
}
stories-heretic-ui-path = Path:
stories-heretic-ui-ritual = The Ritual of Knowledge requires: { $items }
stories-heretic-ui-column-general = General
stories-heretic-ui-cost = ◆ { $cost }
stories-heretic-ui-tooltip-cost = Cost: ◆ { $cost }
stories-heretic-ui-learned = Learned.
stories-heretic-ui-available = Click to learn.
stories-heretic-ui-locked = Not available yet.

stories-heretic-ash-rebirth = You drain life from the flames. Victims: { $count }.
stories-heretic-ingredient-fire = matchstick or lighter

stories-heretic-ash-ascension-corpses = The flame needs { $count } more burnt bodies.
stories-heretic-ascension-done = You have already ascended.
stories-heretic-ash-ascension-announcement = Attention! A surge of anomalous activity detected. { $name } has ascended as the Ashlord. Fire will consume everyone.
stories-heretic-ingredient-glass-shard = glass shard
stories-heretic-ingredient-candle = candle
stories-heretic-ingredient-mask = breath, gas or sterile mask
stories-heretic-ingredient-stunbaton = stun baton
stories-heretic-ingredient-liver = liver
stories-heretic-ingredient-table-or-suit = table or any outer clothing
stories-heretic-ingredient-any-mask = any mask
stories-heretic-mantle-flames-on = The mantle flares up.
stories-heretic-mantle-flames-off = The mantle's flames die down.
stories-heretic-mantle-burns = The mantle bursts into flame and bites into your flesh!
stories-heretic-mantle-limb-burnt = The mantle burns your limb to ash!
stories-guide-entry-heretic = Heretic
stories-heretic-aura-examine = [color=#3fbf3f]{ CAPITALIZE(SUBJECT($ent)) } { CONJUGATE-BE($ent) } crackling with a swirling green vortex of energy.[/color]
stories-heretic-aura-examine-ascend = [color=#3fbf3f]{ CAPITALIZE(SUBJECT($ent)) } { CONJUGATE-BE($ent) } shedding { POSS-ADJ($ent) } mortal shell![/color]
stories-heretic-sacrifice-already = This soul already belongs to the Mansus.
stories-heretic-head-sacrifice-waived = The Mansus sees no living heads of staff on the station. A head sacrifice is no longer required to ascend.
stories-heretic-admin-verb-make = Make heretic
stories-heretic-admin-verb-make-message = Makes the target a heretic: targets, objectives and the role are granted through the Heretic rule.
stories-heretic-admin-verb-arrivals = The player is on the arrivals shuttle: antagonist roles are not granted there. Wait until it docks.
stories-heretic-admin-verb-failed = Could not make the player a heretic: they are a ghost, on the arrivals shuttle or not a humanoid.
