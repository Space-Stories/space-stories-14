stories-heretic-points-gained = Вы получаете знания Мансуса: +{ $amount }.
stories-heretic-learn-unknown = Такого знания не существует.
stories-heretic-learn-known = Вы уже обладаете этим знанием.
stories-heretic-learn-path-chosen = Вы уже ступили на свой путь.
stories-heretic-learn-wrong-path = Это знание чуждо вашему пути.
stories-heretic-learn-requires = Сначала постигните предшествующее знание.
stories-heretic-learn-ascension-locked = Пир сов закрыл для вас путь Вознесения.
stories-heretic-learn-feast-locked = Вы уже ступили на путь Вознесения.
stories-heretic-learn-ascension-objectives = Сначала выполните свои цели.
stories-heretic-learn-no-points = Недостаточно знаний Мансуса.
stories-roles-antag-heretic-name = Еретик
stories-roles-antag-heretic-objective = Постигайте запретные знания Мансуса, приносите жертвы и вознеситесь.
stories-heretic-role-greeting = Вы — Еретик. Завеса реальности истончилась, и вы слышите шёпот Мансуса. Собирайте влияния, приносите жертвы и вознеситесь. Откройте дерево знаний действием «Знания Мансуса».
stories-heretic-round-end-agent-name = еретик
stories-heretic-title = Еретики
stories-heretic-description = На станции есть те, кто слышит зов Мансуса.
stories-heretic-round-end-count = Еретиков было: { $count }.
stories-heretic-round-end-entry = { $name } ({ $user }) — путь: { $path }, знаний: { $knowledge }, жертв: { $sacrifices }.
stories-heretic-round-end-ascended = [color=#58a65c]{ $name } ({ $user }) вознёсся![/color] Путь: { $path }, знаний: { $knowledge }, жертв: { $sacrifices }.
stories-heretic-round-end-lost = { $name } ({ $user }) — тело еретика утрачено.
stories-heretic-path-none = не выбран
stories-heretic-path-STHereticPathAsh = Пепел
stories-heretic-objective-issuer = [color=#58a65c]Мансус[/color]
stories-heretic-objective-knowledge = Изучить { $count } знаний Мансуса.
stories-heretic-objective-sacrifice = Принести в жертву { $count } целей.
stories-heretic-objective-head-sacrifice = { $count ->
    [0] Жертва главы отдела не требуется: живых глав на станции не осталось.
   *[other] Принести в жертву главу отдела ({ $count }).
}

stories-heretic-rune-need-pen = Нужна ручка или другой пишущий предмет в активной руке.
stories-heretic-rune-bad-tile = Здесь нельзя начертить руну.
stories-heretic-rune-too-close = Слишком близко к другой руне.
stories-heretic-ritual-success = Руна вспыхивает, и ритуал свершается.
stories-heretic-ritual-nothing = Руна безмолвствует: ни один ритуал не подходит.
stories-heretic-ritual-limit = Мансус не даст вам больше таких предметов.
stories-heretic-ritual-missing = Не хватает: { $item } ×{ $amount }.
stories-heretic-tag-Knife = нож
stories-heretic-tag-Candle = свеча
stories-heretic-tag-Book = книга
stories-heretic-tag-GlassShard = осколок стекла
stories-heretic-tag-Matchstick = спичка
stories-heretic-tag-Write = пишущий предмет
stories-heretic-tag-Sheet = лист материала
stories-heretic-ingredient-knife = нож
stories-heretic-no-focus = Без фокуса это заклинание не подвластно вам.
stories-heretic-codex-open = Вы раскрываете Кодекс. Страницы шепчут.
stories-heretic-codex-close = Вы закрываете Кодекс.
stories-heretic-ingredient-glass-sheet = лист стекла
stories-heretic-ingredient-eyes = глаза
stories-heretic-ingredient-book = книга
stories-heretic-ingredient-pen = пишущий предмет
stories-heretic-ingredient-hide = шкура или кожа

stories-heretic-grasp-blocked = Святая сила отталкивает Хватку!
stories-heretic-blade-no-place = Мансусу некуда вас перенести.
stories-heretic-sacrifice-not-target = Мансус не примет эту жертву.
stories-heretic-sacrifice-resisting = Жертва ещё сопротивляется.
stories-heretic-sacrifice-victim = Вас утягивает сквозь завесу реальности...
stories-heretic-sacrifice-returned = Вы возвращаетесь. Вы не помните, кто это сделал.
stories-heretic-ingredient-poppy = мак
stories-heretic-ingredient-clothing = одежда
stories-heretic-heart-no-targets = Ваше сердце молчит: целей нет.
stories-heretic-heart-other-map = { $target } — где-то далеко, за пределами этого места.
stories-heretic-heart-direction = { $target }: { $direction }, { $range }.
stories-heretic-range-near = рядом
stories-heretic-range-medium = неподалёку
stories-heretic-range-far = далеко
stories-heretic-direction-North = на севере
stories-heretic-direction-NorthEast = на северо-востоке
stories-heretic-direction-East = на востоке
stories-heretic-direction-SouthEast = на юго-востоке
stories-heretic-direction-South = на юге
stories-heretic-direction-SouthWest = на юго-западе
stories-heretic-direction-West = на западе
stories-heretic-direction-NorthWest = на северо-западе
stories-heretic-direction-Invalid = прямо здесь

stories-heretic-pierced-examine = [color=#c0392b]От одного взгляда на это кружится голова.[/color]
stories-heretic-pierced-examine-heretic = Вы уже испили знание из этого разлома.
stories-heretic-pierced-touch = Что-то по ту сторону хватает вас!

stories-heretic-ui-title = Знания Мансуса
stories-heretic-ui-points = ◆ { $points } { $points ->
    [one] очко
    [few] очка
   *[other] очков
} знаний
stories-heretic-ui-path = Путь:
stories-heretic-ui-ritual = Ритуал знаний требует: { $items }
stories-heretic-ui-column-general = Общие
stories-heretic-ui-cost = ◆ { $cost }
stories-heretic-ui-tooltip-cost = Цена: ◆ { $cost }
stories-heretic-ui-learned = Изучено.
stories-heretic-ui-available = Нажмите, чтобы изучить.
stories-heretic-ui-locked = Пока недоступно.

stories-heretic-ash-rebirth = Вы вытягиваете жизнь из пламени. Жертв: { $count }.
stories-heretic-ingredient-fire = спичка или зажигалка

stories-heretic-ash-ascension-corpses = Пламени нужно ещё { $count } сгоревших тел.
stories-heretic-ascension-done = Вы уже вознеслись.
stories-heretic-ash-ascension-announcement = Внимание! Обнаружен всплеск аномальной активности. { $name } вознёсся как Повелитель пепла. Огонь поглотит всех.
stories-heretic-ingredient-glass-shard = осколок стекла
stories-heretic-ingredient-candle = свеча
stories-heretic-ingredient-mask = дыхательная, противогазная или стерильная маска
stories-heretic-ingredient-stunbaton = станбатон
stories-heretic-ingredient-liver = печень
stories-heretic-ingredient-table-or-suit = стол или любая верхняя одежда
stories-heretic-ingredient-any-mask = любая маска
stories-heretic-mantle-flames-on = Мантия разгорается.
stories-heretic-mantle-flames-off = Пламя мантии гаснет.
stories-heretic-mantle-burns = Мантия вспыхивает и вгрызается в вашу плоть!
stories-heretic-mantle-limb-burnt = Мантия сжигает вашу конечность дотла!
stories-guide-entry-heretic = Еретик
stories-heretic-aura-examine = [color=#3fbf3f]Вокруг { GENDER($ent) ->
    [female] неё
    [epicene] них
   *[other] него
} трещит зелёный вихрь энергии.[/color]
stories-heretic-aura-examine-ascend = [color=#3fbf3f]{ CAPITALIZE(SUBJECT($ent)) } { CONJUGATE-BASIC($ent, "сбрасывают", "сбрасывает") } смертную оболочку![/color]
stories-heretic-sacrifice-already = Эта душа уже принадлежит Мансусу.
stories-heretic-head-sacrifice-waived = Мансус не видит на станции живых глав. Жертва главы для Вознесения больше не требуется.
stories-heretic-admin-verb-make = Сделать еретиком
stories-heretic-admin-verb-make-message = Делает цель еретиком: цели, задачи и роль выдаются через правило «Еретик».
stories-heretic-admin-verb-arrivals = Игрок на шаттле прибытия: на нём роль антагониста не выдаётся. Дождитесь стыковки.
stories-heretic-admin-verb-failed = Не удалось сделать игрока еретиком: он в призраке, на шаттле прибытия или не гуманоид.
