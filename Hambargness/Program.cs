using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Image sources to proxy (avoids CORS issues)
var imageUrls = new Dictionary<string, string>
{
    ["satya"] = "https://pbs.twimg.com/profile_images/1221837516816306177/_Ld4un5A_400x400.jpg",
    ["gates"] = "https://upload.wikimedia.org/wikipedia/commons/a/a8/Bill_Gates_2017_%28cropped%29.jpg",
    ["bezos"] = "https://upload.wikimedia.org/wikipedia/commons/0/03/Jeff_Bezos_visits_LAAFB_SMC_%283908618%29_%28cropped%29.jpeg",
    ["jobs"] = "https://upload.wikimedia.org/wikipedia/commons/d/dc/Steve_Jobs_Headshot_2010-CROP_%28cropped_2%29.jpg",
    ["ballmer"] = "https://upload.wikimedia.org/wikipedia/commons/5/54/Steve_ballmer_2007_outdoors2-2.jpg",
    ["ellison"] = "https://upload.wikimedia.org/wikipedia/commons/0/00/Larry_Ellison_picture.png",
    ["musk"] = "https://upload.wikimedia.org/wikipedia/commons/5/5e/Elon_Musk_-_54820081119_%28cropped%29.jpg"
};

var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; Hambargness/1.0)");

// Cache downloaded images in memory
var imageCache = new Dictionary<string, (byte[] Data, string ContentType)>();

app.MapGet("/img/{name}", async (string name) =>
{
    if (!imageUrls.ContainsKey(name)) return Results.NotFound();

    if (!imageCache.ContainsKey(name))
    {
        try
        {
            var response = await httpClient.GetAsync(imageUrls[name]);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsByteArrayAsync();
            var ct = response.Content.Headers.ContentType?.ToString() ?? "image/jpeg";
            imageCache[name] = (data, ct);
        }
        catch
        {
            return Results.NotFound();
        }
    }

    var cached = imageCache[name];
    return Results.File(cached.Data, cached.ContentType);
});

app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
    <title>Hambargness - Solitaire Win!</title>
    <style>
        :root { --bg: #008000; --felt: #008000; --panel-bg: rgba(255,255,255,0.95); --panel-text: #222; --panel-border: #ccc; --btn-bg: #2d7d2d; --btn-hover: #1b5e1b; --btn-text: #fff; --check-accent: #2d7d2d; }
        .dark { --bg: #1a1a2e; --felt: #16213e; --panel-bg: rgba(30,30,50,0.95); --panel-text: #e0e0e0; --panel-border: #444; --btn-bg: #0f3460; --btn-hover: #1a5276; --btn-text: #fff; --check-accent: #4fc3f7; }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { background: var(--bg); overflow: hidden; width: 100vw; height: 100vh; font-family: 'Segoe UI', system-ui, sans-serif; transition: background 0.3s; }
        canvas { display: block; }

        #settings {
            position: fixed; inset: 0; display: flex; align-items: center; justify-content: center; z-index: 10;
            background: var(--bg); transition: background 0.3s;
        }
        .panel {
            background: var(--panel-bg); color: var(--panel-text); border-radius: 16px; padding: 36px 40px;
            box-shadow: 0 8px 32px rgba(0,0,0,0.3); min-width: 340px; max-width: 420px; border: 1px solid var(--panel-border);
            transition: background 0.3s, color 0.3s, border-color 0.3s;
        }
        .title-box {
            background: linear-gradient(135deg, #ff6b6b, #feca57, #48dbfb, #ff9ff3, #54a0ff, #5f27cd);
            background-size: 300% 300%;
            animation: gradient-shift 4s ease infinite;
            border-radius: 12px; padding: 20px 24px; margin-bottom: 16px; text-align: center;
            box-shadow: 0 4px 20px rgba(0,0,0,0.25);
        }
        @keyframes gradient-shift { 0%{background-position:0% 50%} 50%{background-position:100% 50%} 100%{background-position:0% 50%} }
        .title-box h1 { font-size: 38px; color: #fff; text-shadow: 2px 2px 6px rgba(0,0,0,0.4); margin: 0; letter-spacing: 2px; }
        .title-box .sub { font-size: 14px; color: rgba(255,255,255,0.85); margin: 4px 0 0; text-shadow: 1px 1px 3px rgba(0,0,0,0.3); }
        .section-label { font-weight: 600; font-size: 14px; text-transform: uppercase; letter-spacing: 1px; opacity: 0.5; margin-bottom: 10px; }
        .theme-row { display: flex; gap: 8px; margin-bottom: 24px; }
        .theme-btn {
            flex: 1; padding: 10px; border-radius: 8px; border: 2px solid var(--panel-border); cursor: pointer;
            font-size: 14px; font-weight: 600; text-align: center; transition: all 0.2s; background: transparent; color: var(--panel-text);
        }
        .theme-btn.active { border-color: var(--check-accent); background: var(--check-accent); color: #fff; }
        .theme-btn:hover:not(.active) { border-color: var(--check-accent); }
        .card-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin-bottom: 24px; }
        .card-option {
            display: flex; align-items: center; gap: 10px; padding: 10px 12px; border-radius: 8px; cursor: pointer;
            border: 1px solid var(--panel-border); transition: all 0.2s; user-select: none;
        }
        .card-option:hover { border-color: var(--check-accent); }
        .card-option input { accent-color: var(--check-accent); width: 18px; height: 18px; cursor: pointer; }
        .card-option label { cursor: pointer; font-size: 14px; font-weight: 500; }
        .select-links { text-align: right; margin-bottom: 12px; font-size: 12px; }
        .select-links a { color: var(--check-accent); cursor: pointer; text-decoration: none; margin-left: 12px; }
        .select-links a:hover { text-decoration: underline; }
        #go-btn {
            width: 100%; padding: 14px; border: none; border-radius: 10px; font-size: 18px; font-weight: 700;
            background: var(--btn-bg); color: var(--btn-text); cursor: pointer; transition: background 0.2s; letter-spacing: 0.5px;
        }
        #go-btn:hover { background: var(--btn-hover); }
        #go-btn:disabled { opacity: 0.4; cursor: not-allowed; }
    </style>
</head>
<body>
<div id="settings">
    <div class="panel">
        <div class="title-box">
        <h1>&#127183; Hambargness</h1>
        <p class="sub">Solitaire Win Screen &mdash; Pick Your Cards</p>
    </div>

        <div class="section-label">Theme</div>
        <div class="theme-row">
            <div class="theme-btn active" onclick="setTheme('light')" id="theme-light">&#9728;&#65039; Light</div>
            <div class="theme-btn" onclick="setTheme('dark')" id="theme-dark">&#127769; Dark</div>
        </div>

        <div class="section-label">Cards</div>
        <div class="select-links"><a onclick="toggleAll(true)">Select All</a><a onclick="toggleAll(false)">Deselect All</a></div>
        <div class="card-grid" id="card-grid"></div>

        <button id="go-btn" onclick="launch()">Deal! &#9824;&#65039;</button>
        <div style="margin-top:12px; text-align:center;">
            <a href="/benny" style="color:var(--check-accent); font-size:14px; font-weight:600; text-decoration:none;">&#127183; Play Benny (Card Game) &rarr;</a>
        </div>
    </div>
</div>
<canvas id="c" style="display:none"></canvas>
<script>
const allPeople = [
    { key: 'satya',   label: 'Satya Nadella',  name: 'SATYA\nNADELLA',  color: '#1a6fb5' },
    { key: 'gates',   label: 'Bill Gates',     name: 'BILL\nGATES',     color: '#4a2d8a' },
    { key: 'bezos',   label: 'Jeff Bezos',     name: 'JEFF\nBEZOS',     color: '#c45500' },
    { key: 'jobs',    label: 'Steve Jobs',     name: 'STEVE\nJOBS',     color: '#333333' },
    { key: 'ballmer', label: 'Steve Ballmer',  name: 'STEVE\nBALLMER',  color: '#d42020' },
    { key: 'ellison', label: 'Larry Ellison',  name: 'LARRY\nELLISON',  color: '#cc0000' },
    { key: 'musk',    label: 'Elon Musk',      name: 'ELON\nMUSK',      color: '#1a1a2e' }
];

let darkMode = false;

// Build card checkboxes
const grid = document.getElementById('card-grid');
allPeople.forEach((p, i) => {
    const div = document.createElement('div');
    div.className = 'card-option';
    div.innerHTML = `<input type="checkbox" id="chk${i}" checked><label for="chk${i}">${p.label}</label>`;
    div.addEventListener('click', (e) => { if (e.target.tagName !== 'INPUT') document.getElementById('chk'+i).click(); updateBtn(); });
    div.querySelector('input').addEventListener('change', updateBtn);
    grid.appendChild(div);
});

function updateBtn() {
    const any = allPeople.some((_, i) => document.getElementById('chk'+i).checked);
    document.getElementById('go-btn').disabled = !any;
}

function toggleAll(on) {
    allPeople.forEach((_, i) => document.getElementById('chk'+i).checked = on);
    updateBtn();
}

function setTheme(t) {
    darkMode = t === 'dark';
    document.body.classList.toggle('dark', darkMode);
    document.getElementById('theme-light').classList.toggle('active', !darkMode);
    document.getElementById('theme-dark').classList.toggle('active', darkMode);
}

function launch() {
    const selected = allPeople.filter((_, i) => document.getElementById('chk'+i).checked);
    if (!selected.length) return;
    document.getElementById('settings').style.display = 'none';

    const canvas = document.getElementById('c');
    canvas.style.display = 'block';
    const ctx = canvas.getContext('2d');
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;

    const feltColor = darkMode ? '#16213e' : '#008000';
    document.body.style.background = feltColor;

    const cardW = 100, cardH = 130;
    const cards = [];

    const people = selected.map(p => ({ name: p.name, color: p.color, loaded: false, img: new Image() }));

    let loadCount = 0;
    let started = false;
    function tryStart() { if (!started) { started = true; go(); } }
    people.forEach((p, i) => {
        p.img.onload = () => { p.loaded = true; loadCount++; if (loadCount === people.length) tryStart(); };
        p.img.onerror = () => { loadCount++; if (loadCount === people.length) tryStart(); };
        p.img.src = '/img/' + selected[i].key;
    });
    setTimeout(tryStart, 3000);

    function go() {
        ctx.fillStyle = feltColor;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        setInterval(spawnCard, 120);
        update();
    }

    function spawnCard() {
        cards.push({
            x: Math.random() * (canvas.width - cardW), y: -cardH,
            vx: (Math.random() - 0.5) * 6, vy: Math.random() * 2 + 1,
            gravity: 0.15 + Math.random() * 0.1, bounced: false,
            person: Math.floor(Math.random() * people.length)
        });
    }

    function drawCard(x, y, idx) {
        const p = people[idx];
        ctx.save();
        ctx.shadowColor = 'rgba(0,0,0,0.4)'; ctx.shadowBlur = 6; ctx.shadowOffsetX = 3; ctx.shadowOffsetY = 3;
        ctx.fillStyle = '#fff';
        ctx.beginPath(); ctx.roundRect(x, y, cardW, cardH, 6); ctx.fill();
        ctx.beginPath(); ctx.roundRect(x + 4, y + 4, cardW - 8, cardH - 8, 4); ctx.clip();
        if (p.loaded) {
            ctx.drawImage(p.img, x + 4, y + 4, cardW - 8, cardH - 8);
        } else {
            ctx.fillStyle = p.color; ctx.fillRect(x + 4, y + 4, cardW - 8, cardH - 8);
            ctx.fillStyle = '#fff'; ctx.font = 'bold 11px sans-serif'; ctx.textAlign = 'center';
            const lines = p.name.split('\n');
            ctx.fillText(lines[0], x + cardW/2, y + cardH/2 - 6);
            ctx.fillText(lines[1], x + cardW/2, y + cardH/2 + 10);
        }
        ctx.restore();
    }

    function update() {
        for (const c of cards) {
            c.vy += c.gravity; c.x += c.vx; c.y += c.vy;
            if (c.y + cardH > canvas.height) { c.y = canvas.height - cardH; c.vy = -c.vy * 0.6; c.vx *= 0.95; c.bounced = true; }
            if (c.x < 0) { c.x = 0; c.vx = -c.vx * 0.8; }
            if (c.x + cardW > canvas.width) { c.x = canvas.width - cardW; c.vx = -c.vx * 0.8; }
            drawCard(c.x, c.y, c.person);
        }
        for (let i = cards.length - 1; i >= 0; i--) {
            if (cards[i].bounced && Math.abs(cards[i].vy) < 0.5 && cards[i].y + cardH >= canvas.height - 1) cards.splice(i, 1);
        }
        requestAnimationFrame(update);
    }

    window.addEventListener('resize', () => {
        canvas.width = window.innerWidth; canvas.height = window.innerHeight;
        ctx.fillStyle = feltColor; ctx.fillRect(0, 0, canvas.width, canvas.height);
    });
}
</script>
</body>
</html>
""", "text/html"));

// ===================== BENNY MULTIPLAYER GAME =====================
// Server-side game state (like bedlam-digital)

var bennyLobbies = new ConcurrentBag<BennyLobby>();
var bennyPlayers = new ConcurrentBag<BennyPlayer>();
var rng = new Random();

string[] lobbyNames = ["Redmond","Cupertino","Seattle","Menlo","Austin","Nashville","Denver","Portland","Chicago","Boston","Atlanta","Dallas","Phoenix","Miami","Orlando"];

void EnsureBennyLobby()
{
    if (!bennyLobbies.Any(l => l.Players.Count < 6 && l.Round == 0))
    {
        var name = lobbyNames[rng.Next(lobbyNames.Length)] + "-" + rng.Next(100, 999);
        bennyLobbies.Add(new BennyLobby(name));
    }
}
EnsureBennyLobby();

// JWT helper
bool BennyAuth(HttpContext ctx, out BennyPlayer? player, out BennyLobby? lobby)
{
    player = null; lobby = null;
    var jwt = ctx.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
    if (string.IsNullOrEmpty(jwt)) return false;
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);
        var pid = token.Claims.FirstOrDefault(c => c.Type == "PlayerId")?.Value;
        player = bennyPlayers.FirstOrDefault(p => p.Id == pid);
        if (player == null) return false;
        var vp = new TokenValidationParameters
        {
            ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = true,
            ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(player.SecretKey)
        };
        handler.ValidateToken(jwt, vp, out _);
        var lid = token.Claims.FirstOrDefault(c => c.Type == "LobbyId")?.Value;
        lobby = bennyLobbies.FirstOrDefault(l => l.Id == lid);
        return lobby != null;
    }
    catch { return false; }
}

// List lobbies
app.MapGet("/benny/api/lobbies", () =>
{
    EnsureBennyLobby();
    return Results.Ok(bennyLobbies.Select(l => new { l.Id, Players = l.Players.Select(p => p.Name).ToList(), l.Players.Count, Max = 6, l.Round, l.MaxRounds, Started = l.Round > 0 }));
});

// Join lobby
app.MapGet("/benny/api/join/{lobbyId}/{playerName}", (string lobbyId, string playerName) =>
{
    var lobby = bennyLobbies.FirstOrDefault(l => l.Id == lobbyId);
    if (lobby == null) return Results.NotFound("Lobby not found");
    if (lobby.Round > 0) return Results.BadRequest("Game already started");
    if (lobby.Players.Count >= 6) return Results.BadRequest("Lobby full");
    if (lobby.Players.Any(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))) return Results.BadRequest("Name taken");

    var player = new BennyPlayer(playerName);
    lobby.Players.Add(player);
    bennyPlayers.Add(player);
    EnsureBennyLobby();
    return Results.Ok(new { token = player.GenerateJwt(lobby.Id) });
});

// Start game
app.MapPost("/benny/api/start", (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var p, out var lobby)) return Results.Unauthorized();
    if (lobby!.Players.Count < 2) return Results.BadRequest("Need at least 2 players");
    if (lobby.Round > 0) return Results.BadRequest("Already started");
    lobby.StartRound();
    return Results.Ok();
});

// Set max rounds (first player only, before game starts)
app.MapPost("/benny/api/rounds/{count:int}", (HttpContext ctx, int count) =>
{
    if (!BennyAuth(ctx, out var p, out var lobby)) return Results.Unauthorized();
    if (lobby!.Round > 0) return Results.BadRequest("Already started");
    if (lobby.Players[0].Id != p!.Id) return Results.BadRequest("Only the first player can set rounds");
    if (count < 1 || count > 13) return Results.BadRequest("1-13 rounds");
    lobby.MaxRounds = count;
    return Results.Ok();
});

// Get game state (polled by client)
app.MapGet("/benny/api/state", (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    lobby!.CheckTurnTimeout();
    var me = player!;
    return Results.Ok(new
    {
        lobby.Id, lobby.Round, lobby.MaxRounds, BennyRank = lobby.BennyRankName,
        CurrentTurn = lobby.CurrentTurnPlayerName,
        IsMyTurn = lobby.IsPlayerTurn(me.Id),
        Phase = lobby.GetPhaseForPlayer(me.Id),
        MyHand = me.Hand.Select(c => new { c.Rank, c.Suit, c.Id, IsBenny = c.Rank == lobby.BennyRank, IsNew = c.Id == me.LastDrawnCardId }).ToList(),
        MyMelds = me.Melds.Select(m => m.Select(c => new { c.Rank, c.Suit, c.Id, IsBenny = c.Rank == lobby.BennyRank }).ToList()).ToList(),
        HasMelds = me.Melds.Count > 0,
        AllMelds = lobby.Players.Select(pl => new { pl.Name, IsMe = pl.Id == me.Id, Melds = pl.Melds.Select((m, mi) => new { Index = mi, Cards = m.Select(c => new { c.Rank, c.Suit, c.Id, IsBenny = c.Rank == lobby.BennyRank }).ToList() }).ToList() }).ToList(),
        DiscardTop = lobby.DiscardPile.Count > 0 ? new { lobby.DiscardPile.Last().Rank, lobby.DiscardPile.Last().Suit, lobby.DiscardPile.Last().Id, IsBenny = lobby.DiscardPile.Last().Rank == lobby.BennyRank } : null,
        DeckCount = lobby.Deck.Count,
        Players = lobby.Players.Select(pl => new { pl.Name, HandCount = pl.Hand.Count, MeldCount = pl.Melds.Count, pl.Score, IsCurrentTurn = lobby.IsPlayerTurn(pl.Id) }).ToList(),
        TurnSeconds = lobby.GetTurnSecondsRemaining(),
        lobby.RoundOver, RoundWinner = lobby.RoundWinnerName,
        lobby.GameOver, GameWinner = lobby.GameWinnerName,
        Messages = lobby.Messages.TakeLast(20).ToList()
    });
});

// Draw from deck
app.MapPost("/benny/api/draw/deck", (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    if (!lobby!.IsPlayerTurn(player!.Id)) return Results.BadRequest("Not your turn");
    if (lobby.GetPhaseForPlayer(player.Id) != "draw") return Results.BadRequest("Already drew");
    if (lobby.Deck.Count == 0) return Results.BadRequest("Deck empty");
    lock (lobby.Lock) { var c = lobby.Deck[0]; player.Hand.Add(c); lobby.Deck.RemoveAt(0); player.HasDrawn = true; player.LastDrawnCardId = c.Id; }
    lobby.AddMessage($"{player.Name} drew from deck");
    return Results.Ok();
});

// Draw from discard
app.MapPost("/benny/api/draw/discard", (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    if (!lobby!.IsPlayerTurn(player!.Id)) return Results.BadRequest("Not your turn");
    if (lobby.GetPhaseForPlayer(player.Id) != "draw") return Results.BadRequest("Already drew");
    if (lobby.DiscardPile.Count == 0) return Results.BadRequest("Discard empty");
    lock (lobby.Lock) { var c = lobby.DiscardPile.Last(); lobby.DiscardPile.Remove(c); player.Hand.Add(c); player.HasDrawn = true; player.LastDrawnCardId = c.Id; }
    lobby.AddMessage($"{player.Name} picked up discard");
    return Results.Ok();
});

// Lay meld
app.MapPost("/benny/api/meld", async (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    if (!lobby!.IsPlayerTurn(player!.Id)) return Results.BadRequest("Not your turn");
    if (lobby.GetPhaseForPlayer(player.Id) != "play") return Results.BadRequest("Draw first");
    var body = await JsonSerializer.DeserializeAsync<string[]>(ctx.Request.Body);
    if (body == null || body.Length < 3) return Results.BadRequest("Need 3+ cards");
    var cards = body.Select(id => player.Hand.FirstOrDefault(c => c.Id == id)).Where(c => c != null).ToList();
    if (cards.Count != body.Length) return Results.BadRequest("Cards not in hand");
    if (!BennyLobby.IsValidMeld(cards!, lobby.BennyRank)) return Results.BadRequest("Invalid meld");
    lock (lobby.Lock) { player.Melds.Add(cards!); foreach (var c in cards!) player.Hand.Remove(c); }
    lobby.AddMessage($"{player.Name} laid a meld of {cards!.Count} cards");
    if (player.Hand.Count == 0) { lobby.EndRound(player); return Results.Ok(new { went_out = true }); }
    return Results.Ok(new { went_out = false });
});

// Discard
app.MapPost("/benny/api/discard/{cardId}", (HttpContext ctx, string cardId) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    if (!lobby!.IsPlayerTurn(player!.Id)) return Results.BadRequest("Not your turn");
    if (lobby.GetPhaseForPlayer(player.Id) != "play") return Results.BadRequest("Draw first");
    var card = player.Hand.FirstOrDefault(c => c.Id == cardId);
    if (card == null) return Results.BadRequest("Card not in hand");
    lock (lobby.Lock) { player.Hand.Remove(card); lobby.DiscardPile.Add(card); }
    lobby.AddMessage($"{player.Name} discarded {card.Rank}{card.Suit}");
    if (player.Hand.Count == 0) { lobby.EndRound(player); }
    else { lobby.NextTurn(); }
    return Results.Ok();
});

// Lay off card(s) onto an existing meld (any player's)
app.MapPost("/benny/api/layoff/{playerName}/{meldIndex:int}", async (HttpContext ctx, string playerName, int meldIndex) =>
{
    if (!BennyAuth(ctx, out var player, out var lobby)) return Results.Unauthorized();
    if (!lobby!.IsPlayerTurn(player!.Id)) return Results.BadRequest("Not your turn");
    if (lobby.GetPhaseForPlayer(player.Id) != "play") return Results.BadRequest("Draw first");
    if (player.Melds.Count == 0) return Results.BadRequest("You must lay at least one meld first");
    var targetPlayer = lobby.Players.FirstOrDefault(p => p.Name == playerName);
    if (targetPlayer == null) return Results.BadRequest("Player not found");
    if (meldIndex < 0 || meldIndex >= targetPlayer.Melds.Count) return Results.BadRequest("Invalid meld");
    var body = await JsonSerializer.DeserializeAsync<string[]>(ctx.Request.Body);
    if (body == null || body.Length == 0) return Results.BadRequest("No cards specified");
    var cards = body.Select(id => player.Hand.FirstOrDefault(c => c.Id == id)).Where(c => c != null).ToList();
    if (cards.Count != body.Length) return Results.BadRequest("Cards not in hand");
    // Validate: meld + new cards must still be valid
    var combined = new List<BennyCard>(targetPlayer.Melds[meldIndex]);
    combined.AddRange(cards!);
    if (!BennyLobby.IsValidMeld(combined, lobby.BennyRank)) return Results.BadRequest("Cards don't fit this meld");
    lock (lobby.Lock) { targetPlayer.Melds[meldIndex] = combined; foreach (var c in cards!) player.Hand.Remove(c); }
    lobby.AddMessage($"{player.Name} laid off {cards!.Count} card(s) onto {targetPlayer.Name}'s meld");
    if (player.Hand.Count == 0) { lobby.EndRound(player); return Results.Ok(new { went_out = true }); }
    return Results.Ok(new { went_out = false });
});

// Next round (after round over)
app.MapPost("/benny/api/nextround", (HttpContext ctx) =>
{
    if (!BennyAuth(ctx, out var p, out var lobby)) return Results.Unauthorized();
    if (!lobby!.RoundOver) return Results.BadRequest("Round not over");
    lock (lobby.Lock) { if (lobby.Round < lobby.MaxRounds && !lobby.GameOver) lobby.StartRound(); }
    return Results.Ok();
});


// Benny frontend
app.MapGet("/benny", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
<title>Benny - Multiplayer Card Game</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#1a6d1a;font-family:'Segoe UI',system-ui,sans-serif;color:#fff;min-height:100vh}
.lobby-screen,.game-screen{display:none;flex-direction:column;align-items:center;min-height:100vh}
.lobby-screen.active,.game-screen.active{display:flex}

/* Lobby */
.lobby-panel{background:rgba(0,0,0,0.4);border-radius:16px;padding:30px 36px;margin:40px auto;max-width:480px;width:90%}
.lobby-panel h1{text-align:center;font-size:32px;margin-bottom:4px}
.lobby-panel .sub{text-align:center;font-size:13px;opacity:0.6;margin-bottom:20px}
.lobby-panel input{width:100%;padding:12px;border-radius:8px;border:2px solid rgba(255,255,255,0.2);background:rgba(255,255,255,0.1);color:#fff;font-size:16px;margin-bottom:12px;outline:none}
.lobby-panel input::placeholder{color:rgba(255,255,255,0.4)}
.lobby-panel input:focus{border-color:#feca57}
.lobby-list{margin:16px 0}
.lobby-item{display:flex;justify-content:space-between;align-items:center;padding:10px 14px;background:rgba(255,255,255,0.08);border-radius:8px;margin-bottom:6px}
.lobby-item .name{font-weight:600}
.lobby-item .count{font-size:13px;opacity:0.7}
.btn{padding:10px 20px;border:none;border-radius:8px;font-size:14px;font-weight:600;cursor:pointer;transition:all 0.2s}
.btn-gold{background:#feca57;color:#333}.btn-gold:hover{background:#f9b824}
.btn-sm{padding:6px 14px;font-size:13px}
.btn:disabled{opacity:0.3;cursor:not-allowed}
.back-link{color:#feca57;text-decoration:none;font-size:14px;margin:16px}
.back-link:hover{text-decoration:underline}
.waiting{text-align:center;padding:16px;opacity:0.7;font-size:14px}
.player-chips{display:flex;gap:6px;flex-wrap:wrap;margin:8px 0}
.chip{background:rgba(255,255,255,0.15);padding:4px 10px;border-radius:12px;font-size:12px}

/* Game */
.top-bar{width:100%;background:rgba(0,0,0,0.35);padding:8px 16px;display:flex;justify-content:space-between;align-items:center;flex-wrap:wrap;gap:8px}
.badge{background:rgba(255,255,255,0.15);padding:4px 12px;border-radius:20px;font-size:12px}
.badge-benny{background:#feca57!important;color:#333!important;font-weight:700}
.badge-turn{background:#27ae60!important}
.game-area{flex:1;width:100%;max-width:960px;padding:8px 12px;display:flex;flex-direction:column;gap:6px;margin:0 auto}
.zone{text-align:center}
.zone-label{font-size:10px;text-transform:uppercase;letter-spacing:1px;opacity:0.5;margin-bottom:4px}
.card-row{display:flex;gap:5px;justify-content:center;flex-wrap:wrap;min-height:80px}
.card{width:62px;height:90px;border-radius:6px;display:flex;flex-direction:column;align-items:center;justify-content:center;font-weight:700;cursor:default;transition:all 0.15s;box-shadow:0 2px 6px rgba(0,0,0,0.3);border:2px solid transparent;flex-shrink:0;position:relative}
.card-w{background:#fff;color:#333}.card-r{background:#fff;color:#c00}
.card-back{background:linear-gradient(135deg,#1a3a8a,#2d5dc7);color:#fff;font-size:10px}
.card.sel{border-color:#feca57;transform:translateY(-6px);box-shadow:0 6px 16px rgba(254,202,87,0.4)}
.card.benny{box-shadow:0 0 10px 2px #feca57}
.card.new-card{border-color:#48dbfb;box-shadow:0 0 12px 3px rgba(72,219,251,0.6);animation:pulse-new 1s ease infinite}
@keyframes pulse-new{0%,100%{box-shadow:0 0 12px 3px rgba(72,219,251,0.6)}50%{box-shadow:0 0 18px 6px rgba(72,219,251,0.9)}}
.card,.meld-group .card{transition:transform 0.3s ease,box-shadow 0.3s ease,opacity 0.3s ease}
@keyframes card-enter{from{opacity:0;transform:translateY(-30px) scale(0.8)}to{opacity:1;transform:translateY(0) scale(1)}}
.card-enter{animation:card-enter 0.35s ease-out}
.meld-owner{font-size:10px;font-weight:600;opacity:0.7;margin-bottom:2px;text-align:center}
.all-melds-zone{display:flex;gap:12px;flex-wrap:wrap;justify-content:center;min-height:40px}
.player-meld-block{display:flex;flex-direction:column;align-items:center;gap:2px}
.meld-group.layoff-target{cursor:pointer;border:2px dashed rgba(254,202,87,0.5);border-radius:6px}
.meld-group.layoff-target:hover{border-color:#feca57;background:rgba(254,202,87,0.1)}
.card .r{font-size:18px;line-height:1}.card .s{font-size:16px}
.card .tl{position:absolute;top:2px;left:4px;font-size:9px;line-height:1.1}.card .br{position:absolute;bottom:2px;right:4px;font-size:9px;line-height:1.1;transform:rotate(180deg)}
.table-row{display:flex;gap:10px;align-items:center;justify-content:center}
.pile-placeholder{width:62px;height:90px;border-radius:6px;border:2px dashed rgba(255,255,255,0.25);display:flex;align-items:center;justify-content:center;font-size:10px;opacity:0.4}
.actions{display:flex;gap:6px;justify-content:center;flex-wrap:wrap}
.msg-box{background:rgba(0,0,0,0.3);padding:6px 14px;border-radius:8px;font-size:13px;text-align:center;min-height:26px}
.opp-row{display:flex;gap:16px;justify-content:center;flex-wrap:wrap}
.opp-block{background:rgba(0,0,0,0.2);border-radius:8px;padding:8px 12px;text-align:center;min-width:100px}
.opp-block .opp-name{font-weight:600;font-size:13px;margin-bottom:4px}
.opp-block .opp-info{font-size:11px;opacity:0.7}
.opp-block.active-turn{border:2px solid #27ae60}
.meld-zone{display:flex;gap:8px;flex-wrap:wrap;justify-content:center;min-height:40px}
.meld-group{display:flex;gap:1px;padding:3px;background:rgba(255,255,255,0.08);border-radius:4px}
.meld-group .card{width:40px;height:58px;font-size:10px}.meld-group .card .r{font-size:13px}.meld-group .card .s{font-size:12px}.meld-group .card .tl,.meld-group .card .br{display:none}
.scoreboard{background:rgba(0,0,0,0.3);border-radius:10px;padding:10px 16px;width:100%;max-width:400px;margin:4px auto}
.scoreboard h3{font-size:13px;text-align:center;margin-bottom:4px}
.scoreboard table{width:100%;font-size:12px;border-collapse:collapse}
.scoreboard td{padding:2px 6px}
.scoreboard .me{color:#feca57;font-weight:700}
.log-box{background:rgba(0,0,0,0.25);border-radius:8px;padding:8px 12px;max-height:100px;overflow-y:auto;font-size:11px;width:100%;max-width:600px;margin:4px auto}
.log-box div{padding:1px 0;opacity:0.8}
.overlay{position:fixed;inset:0;background:rgba(0,0,0,0.7);display:none;align-items:center;justify-content:center;z-index:100}
.overlay.show{display:flex}
.overlay-box{background:#2d5a2d;border-radius:16px;padding:28px 36px;text-align:center;box-shadow:0 8px 32px rgba(0,0,0,0.5);max-width:400px}
.overlay-box h2{font-size:22px;margin-bottom:10px}
.overlay-box p{margin-bottom:14px;opacity:0.8;font-size:14px;white-space:pre-line}
.timer{font-size:20px;font-weight:700;color:#feca57}
</style>
</head>
<body>

<!-- LOBBY SCREEN -->
<div class="lobby-screen active" id="lobby-screen">
    <a href="/" class="back-link">&larr; Back to Hambargness</a>
    <div class="lobby-panel">
        <h1>&#127183; Benny</h1>
        <p class="sub">Multiplayer Rummy &mdash; Wild Card Edition</p>
        <input id="name-input" placeholder="Enter your name..." maxlength="16">
        <div class="zone-label" style="margin-top:8px">Available Tables</div>
        <div class="lobby-list" id="lobby-list"></div>
        <div class="waiting" id="waiting-area" style="display:none">
            <p>Waiting for players... <span id="wait-count"></span></p>
            <div class="player-chips" id="wait-players"></div>
            <div style="margin:12px 0" id="rounds-picker">
                <label style="font-size:13px;font-weight:600;margin-right:8px">Rounds:</label>
                <select id="rounds-select" onchange="setRounds()" style="padding:6px 10px;border-radius:6px;border:1px solid rgba(255,255,255,0.3);background:rgba(255,255,255,0.1);color:#fff;font-size:14px;cursor:pointer">
                    <option value="1">1</option><option value="2">2</option><option value="3">3</option>
                    <option value="4">4</option><option value="5">5</option><option value="6">6</option>
                    <option value="7">7</option><option value="8">8</option><option value="9">9</option>
                    <option value="10">10</option><option value="11">11</option><option value="12">12</option>
                    <option value="13" selected>13</option>
                </select>
            </div>
            <button class="btn btn-gold" id="start-btn" onclick="startGame()" style="margin-top:12px" disabled>Start Game (2+ players)</button>
        </div>
    </div>
</div>

<!-- GAME SCREEN -->
<div class="game-screen" id="game-screen">
    <div class="top-bar">
        <a href="/benny" class="back-link" style="margin:0">&larr; Leave</a>
        <div style="display:flex;gap:6px;flex-wrap:wrap;align-items:center">
            <span class="badge" id="g-round">Round 1/13</span>
            <span class="badge badge-benny" id="g-benny">Benny: A</span>
            <span class="badge" id="g-turn">Waiting</span>
            <span class="timer" id="g-timer">60</span>
        </div>
    </div>
    <div class="game-area">
        <div class="zone"><div class="zone-label">Other Players</div><div class="opp-row" id="opp-row"></div></div>
        <div class="zone"><div class="zone-label">Table</div>
            <div class="table-row">
                <div class="card card-back" id="draw-deck" onclick="drawDeck()" style="cursor:pointer"><span>DRAW</span></div>
                <div id="discard-top" class="pile-placeholder">Empty</div>
            </div>
        </div>
        <div class="msg-box" id="msg">Join a game to start playing.</div>
        <div class="actions">
            <button class="btn btn-gold btn-sm" id="btn-pickup" onclick="drawDiscard()" disabled>Pick Up</button>
            <button class="btn btn-gold btn-sm" id="btn-meld" onclick="layMeld()" disabled>Lay Meld</button>
            <button class="btn btn-gold btn-sm" id="btn-layoff" onclick="toggleLayoff()" disabled style="background:#48dbfb;color:#333">Lay Off</button>
            <button class="btn btn-sm" style="background:#e74c3c;color:#fff" id="btn-discard" onclick="discardSel()" disabled>Discard</button>
        </div>
        <div class="zone"><div class="zone-label">Melds on Table</div><div class="all-melds-zone" id="all-melds"></div></div>
        <div class="zone"><div class="zone-label">Your Hand (click to select)</div><div class="card-row" id="my-hand"></div></div>
        <div class="scoreboard"><h3>Scores</h3><table id="score-table"></table></div>
        <div class="log-box" id="log-box"></div>
    </div>
</div>

<!-- OVERLAY -->
<div class="overlay" id="overlay"><div class="overlay-box"><h2 id="ov-title"></h2><p id="ov-msg"></p><button class="btn btn-gold" id="ov-btn" onclick="handleOverlay()">Next Round</button></div></div>

<!-- WIN ANIMATION CANVAS -->
<canvas id="win-canvas" style="display:none;position:fixed;inset:0;z-index:200"></canvas>

<script>
let token = null, pollId = null, selected = new Set(), lastRound = 0, overlayShown = false, layoffMode = false, prevHandIds = new Set();
const API = '/benny/api';

function h(tag,cls,html){ const e=document.createElement(tag); if(cls)e.className=cls; if(html)e.innerHTML=html; return e; }

// ---- LOBBY ----
async function loadLobbies(){
    try{
        const r = await fetch(API+'/lobbies'); const data = await r.json();
        const el = document.getElementById('lobby-list');
        el.innerHTML='';
        data.forEach(l=>{
            const item = h('div','lobby-item');
            item.innerHTML=`<div><span class="name">${l.id}</span><div class="count">${l.count}/6 players ${l.started?'(playing)':''}</div></div>`;
            if(!l.started && l.count<6){
                const btn = h('button','btn btn-gold btn-sm','Join');
                btn.onclick=()=>joinLobby(l.id);
                item.appendChild(btn);
            }
            el.appendChild(item);
        });
    }catch{}
}
loadLobbies(); setInterval(()=>{ if(!token) loadLobbies(); }, 3000);

async function joinLobby(id){
    const name = document.getElementById('name-input').value.trim();
    if(!name){alert('Enter your name first!');return;}
    try{
        const r = await fetch(`${API}/join/${id}/${encodeURIComponent(name)}`);
        if(!r.ok){alert(await r.text());return;}
        const d = await r.json(); token = d.token;
        document.getElementById('waiting-area').style.display='block';
        document.getElementById('lobby-list').style.display='none';
        document.getElementById('name-input').disabled=true;
        pollWaiting(id);
    }catch(e){alert('Error: '+e);}
}

function pollWaiting(){
    const wid = setInterval(async()=>{
        try{
            const r = await fetch(API+'/state',{headers:{'Authorization':'Bearer '+token}});
            if(!r.ok) return;
            const s = await r.json();
            document.getElementById('wait-count').textContent=`(${s.players.length} joined)`;
            document.getElementById('wait-players').innerHTML=s.players.map(p=>`<span class="chip">${p.name}</span>`).join('');
            document.getElementById('start-btn').disabled = s.players.length < 2;
            // Only first player can set rounds
            const isFirst = s.players.length > 0 && s.players[0].name === s.players.find((_,i)=>i===0)?.name;
            document.getElementById('rounds-select').value = s.maxRounds;
            if(s.round > 0){ clearInterval(wid); enterGame(); }
        }catch{}
    }, 1500);
}

async function setRounds(){
    const v=document.getElementById('rounds-select').value;
    await fetch(API+'/rounds/'+v,{method:'POST',headers:{'Authorization':'Bearer '+token}});
}

async function startGame(){
    await fetch(API+'/start',{method:'POST',headers:{'Authorization':'Bearer '+token}});
}

function enterGame(){
    document.getElementById('lobby-screen').classList.remove('active');
    document.getElementById('game-screen').classList.add('active');
    overlayShown=false; lastRound=0;
    pollId = setInterval(pollState, 1000);
    pollState();
}

// ---- GAME ----
async function pollState(){
    try{
        const r = await fetch(API+'/state',{headers:{'Authorization':'Bearer '+token}});
        if(!r.ok) return;
        const s = await r.json();
        renderState(s);
    }catch{}
}

function cardHtml(c){
    return `<span class="r">${c.rank}</span><span class="s">${c.suit}</span><span class="tl">${c.rank}<br>${c.suit}</span><span class="br">${c.rank}<br>${c.suit}</span>`;
}

let lastState = null;

function renderState(s){
    lastState = s;
    // Top bar
    document.getElementById('g-round').textContent=`Round ${s.round}/${s.maxRounds}`;
    document.getElementById('g-benny').textContent=`Benny: ${s.bennyRank}`;
    document.getElementById('g-turn').textContent=s.isMyTurn?'Your Turn!':s.currentTurn+"'s turn";
    document.getElementById('g-turn').className=`badge ${s.isMyTurn?'badge-turn':''}`;
    document.getElementById('g-timer').textContent=s.turnSeconds;

    // Opponents
    const oppEl = document.getElementById('opp-row');
    oppEl.innerHTML='';
    s.players.forEach(p=>{
        const div=h('div',`opp-block ${p.isCurrentTurn?'active-turn':''}`);
        div.innerHTML=`<div class="opp-name">${p.name}</div><div class="opp-info">${p.handCount} cards | ${p.meldCount} melds | ${p.score} pts</div>`;
        oppEl.appendChild(div);
    });

    // Deck
    document.getElementById('draw-deck').style.display=s.deckCount>0?'flex':'none';

    // Discard
    const dt=document.getElementById('discard-top');
    if(s.discardTop){
        const col=s.discardTop.suit==='♥'||s.discardTop.suit==='♦'?'card-r':'card-w';
        const b=s.discardTop.isBenny?'benny':'';
        dt.className=`card ${col} ${b}`;
        dt.innerHTML=cardHtml(s.discardTop);
    } else { dt.className='pile-placeholder'; dt.innerHTML='Empty'; }

    // My hand — detect newly appeared cards for animation
    const curIds = new Set((s.myHand||[]).map(c=>c.id));
    const mh=document.getElementById('my-hand');
    mh.innerHTML='';
    (s.myHand||[]).forEach((c,i)=>{
        const col=c.suit==='♥'||c.suit==='♦'?'card-r':'card-w';
        const sel=selected.has(c.id)?'sel':'';
        const b=c.isBenny?'benny':'';
        const isNew=c.isNew?'new-card':'';
        const enter=!prevHandIds.has(c.id)?'card-enter':'';
        const div=h('div',`card ${col} ${sel} ${b} ${isNew} ${enter}`);
        div.innerHTML=cardHtml(c);
        div.onclick=()=>{selected.has(c.id)?selected.delete(c.id):selected.add(c.id);renderState(s);};
        mh.appendChild(div);
    });
    prevHandIds=curIds;

    // All melds (all players)
    const am=document.getElementById('all-melds');
    const anyMelds = (s.allMelds||[]).some(p=>p.melds.length>0);
    if(!anyMelds){ am.innerHTML='<span style="opacity:0.3;font-size:11px">No melds yet</span>'; }
    else {
        am.innerHTML='';
        (s.allMelds||[]).forEach(pl=>{
            if(pl.melds.length===0) return;
            const block=h('div','player-meld-block');
            block.innerHTML=`<div class="meld-owner">${pl.isMe?'You':pl.name}</div>`;
            pl.melds.forEach(m=>{
                const canLayoff = layoffMode && s.phase==='play' && s.hasMelds && selected.size>0;
                const mg=h('div',`meld-group ${canLayoff?'layoff-target':''}`);
                mg.innerHTML=m.cards.map(c=>{
                    const col=c.suit==='♥'||c.suit==='♦'?'card-r':'card-w';
                    const b=c.isBenny?'benny':'';
                    return `<div class="card ${col} ${b}">${cardHtml(c)}</div>`;
                }).join('');
                if(canLayoff) mg.onclick=()=>doLayoff(pl.name, m.index);
                block.appendChild(mg);
            });
            am.appendChild(block);
        });
    }

    // Buttons
    document.getElementById('btn-pickup').disabled = s.phase!=='draw'||!s.discardTop;
    document.getElementById('btn-meld').disabled = s.phase!=='play'||selected.size<3;
    document.getElementById('btn-layoff').disabled = s.phase!=='play'||!s.hasMelds||selected.size===0||!anyMelds;
    document.getElementById('btn-discard').disabled = s.phase!=='play'||selected.size!==1;
    document.getElementById('draw-deck').style.cursor = s.phase==='draw'?'pointer':'default';
    document.getElementById('draw-deck').onclick = s.phase==='draw'?drawDeck:null;
    document.getElementById('btn-layoff').textContent = layoffMode?'Cancel Lay Off':'Lay Off';

    // Message
    if(layoffMode) document.getElementById('msg').textContent='Click a meld to lay off your selected card(s) onto it.';
    else document.getElementById('msg').textContent = s.isMyTurn?(s.phase==='draw'?'Draw a card from deck or pick up discard.':'Select cards to meld, lay off, or select one to discard.'):s.currentTurn+"'s turn — waiting...";

    // Scores
    const st=document.getElementById('score-table');
    st.innerHTML=s.players.map(p=>`<tr><td${s.isMyTurn&&p.isCurrentTurn?' class="me"':''}>${p.name}</td><td>${p.score} pts</td></tr>`).join('');

    // Log
    const lb=document.getElementById('log-box');
    lb.innerHTML=(s.messages||[]).map(m=>`<div>${m}</div>`).join('');
    lb.scrollTop=lb.scrollHeight;

    // Round / game over overlay
    if(s.roundOver && !overlayShown){
        overlayShown=true;
        document.getElementById('ov-title').textContent=s.gameOver?'Game Over!':'Round Over!';
        const lines=s.players.map(p=>`${p.name}: ${p.score} pts`).join('\n');
        document.getElementById('ov-msg').textContent=(s.roundWinner?s.roundWinner+' went out!\n\n':'')+lines+(s.gameOver?'\n\n🏆 '+s.gameWinner+' wins!':'');
        document.getElementById('ov-btn').textContent=s.gameOver?'Back to Lobby':'Next Round';
        document.getElementById('overlay').classList.add('show');
    }
    if(!s.roundOver && lastRound>0 && s.round>lastRound){ overlayShown=false; document.getElementById('overlay').classList.remove('show'); layoffMode=false; }
    lastRound=s.round;
}

async function drawDeck(){
    await fetch(API+'/draw/deck',{method:'POST',headers:{'Authorization':'Bearer '+token}});
    selected.clear(); layoffMode=false; pollState();
}
async function drawDiscard(){
    await fetch(API+'/draw/discard',{method:'POST',headers:{'Authorization':'Bearer '+token}});
    selected.clear(); layoffMode=false; pollState();
}
async function layMeld(){
    const ids=[...selected];
    const r=await fetch(API+'/meld',{method:'POST',headers:{'Authorization':'Bearer '+token,'Content-Type':'application/json'},body:JSON.stringify(ids)});
    if(!r.ok){alert(await r.text());return;}
    selected.clear(); layoffMode=false; pollState();
}
function toggleLayoff(){
    layoffMode=!layoffMode;
    if(lastState) renderState(lastState);
}
async function doLayoff(playerName, meldIdx){
    const ids=[...selected];
    const r=await fetch(API+'/layoff/'+encodeURIComponent(playerName)+'/'+meldIdx,{method:'POST',headers:{'Authorization':'Bearer '+token,'Content-Type':'application/json'},body:JSON.stringify(ids)});
    if(!r.ok){alert(await r.text());return;}
    selected.clear(); layoffMode=false; pollState();
}
async function discardSel(){
    const id=[...selected][0];
    await fetch(API+'/discard/'+encodeURIComponent(id),{method:'POST',headers:{'Authorization':'Bearer '+token}});
    selected.clear(); layoffMode=false; pollState();
}

// Solitaire win animation
function startWinAnimation(){
    const cv=document.getElementById('win-canvas');
    cv.style.display='block';
    cv.width=window.innerWidth; cv.height=window.innerHeight;
    const cx=cv.getContext('2d');
    cx.fillStyle='#008000'; cx.fillRect(0,0,cv.width,cv.height);
    const suits=['♠','♥','♦','♣'], ranks=['A','2','3','4','5','6','7','8','9','10','J','Q','K'];
    const cw=70,ch=100,aCards=[];
    const intv=setInterval(()=>{
        const r=ranks[Math.floor(Math.random()*13)], s=suits[Math.floor(Math.random()*4)];
        const red=s==='♥'||s==='♦';
        aCards.push({x:Math.random()*(cv.width-cw),y:-ch,vx:(Math.random()-0.5)*6,vy:Math.random()*2+1,g:0.15+Math.random()*0.1,b:false,r,s,red});
    },100);
    function draw(){
        for(const c of aCards){
            c.vy+=c.g; c.x+=c.vx; c.y+=c.vy;
            if(c.y+ch>cv.height){c.y=cv.height-ch;c.vy=-c.vy*0.6;c.vx*=0.95;c.b=true;}
            if(c.x<0){c.x=0;c.vx=-c.vx*0.8;} if(c.x+cw>cv.width){c.x=cv.width-cw;c.vx=-c.vx*0.8;}
            cx.save(); cx.shadowColor='rgba(0,0,0,0.4)'; cx.shadowBlur=5; cx.shadowOffsetX=2; cx.shadowOffsetY=2;
            cx.fillStyle='#fff'; cx.beginPath(); cx.roundRect(c.x,c.y,cw,ch,5); cx.fill(); cx.restore();
            cx.fillStyle=c.red?'#cc0000':'#333'; cx.font='bold 20px sans-serif'; cx.textAlign='center';
            cx.fillText(c.r,c.x+cw/2,c.y+ch/2-4); cx.font='18px sans-serif'; cx.fillText(c.s,c.x+cw/2,c.y+ch/2+18);
        }
        for(let i=aCards.length-1;i>=0;i--){if(aCards[i].b&&Math.abs(aCards[i].vy)<0.5&&aCards[i].y+ch>=cv.height-1)aCards.splice(i,1);}
        requestAnimationFrame(draw);
    }
    draw();
    // Add click-to-dismiss after 3 seconds
    setTimeout(()=>{
        cv.onclick=()=>{ clearInterval(intv); cv.style.display='none'; cv.onclick=null; };
        // Show a hint
        cx.save(); cx.fillStyle='rgba(0,0,0,0.5)'; cx.fillRect(cv.width/2-120,20,240,36); cx.fillStyle='#fff'; cx.font='16px sans-serif'; cx.textAlign='center'; cx.fillText('Click anywhere to dismiss',cv.width/2,44); cx.restore();
    },3000);
}

async function handleOverlay(){
    const btn=document.getElementById('ov-btn');
    if(btn.textContent==='Back to Lobby'){
        startWinAnimation();
        document.getElementById('overlay').classList.remove('show');
        setTimeout(()=>location.reload(), 12000);
        return;
    }
    await fetch(API+'/nextround',{method:'POST',headers:{'Authorization':'Bearer '+token}});
    overlayShown=false; document.getElementById('overlay').classList.remove('show'); pollState();
}
</script>
</body>
</html>
""", "text/html"));

app.Run();

// Benny game classes (all in one file like bedlam-digital)
public class BennyCard
{
    public string Rank { get; set; } = "";
    public string Suit { get; set; } = "";
    public string Id => Rank + Suit;
}

public class BennyPlayer
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public byte[] SecretKey { get; } = Encoding.UTF8.GetBytes(Guid.NewGuid().ToString() + Guid.NewGuid().ToString());
    public List<BennyCard> Hand { get; set; } = new();
    public List<List<BennyCard>> Melds { get; set; } = new();
    public int Score { get; set; } = 0;
    public bool HasDrawn { get; set; } = false;
    public string? LastDrawnCardId { get; set; }

    public BennyPlayer(string name) { Name = name; }

    public string GenerateJwt(string lobbyId)
    {
        var claims = new List<Claim> { new("PlayerId", Id), new("PlayerName", Name), new("LobbyId", lobbyId) };
        var token = new JwtSecurityToken("Benny", "Benny", claims, expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(SecretKey), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class BennyLobby
{
    static readonly string[] Suits = ["♠", "♥", "♦", "♣"];
    static readonly string[] Ranks = ["A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K"];
    static readonly Dictionary<string, int> RankVal = new() { ["A"] = 1, ["2"] = 2, ["3"] = 3, ["4"] = 4, ["5"] = 5, ["6"] = 6, ["7"] = 7, ["8"] = 8, ["9"] = 9, ["10"] = 10, ["J"] = 10, ["Q"] = 10, ["K"] = 10 };

    public string Id { get; }
    public List<BennyPlayer> Players { get; } = new();
    public List<BennyCard> Deck { get; set; } = new();
    public List<BennyCard> DiscardPile { get; set; } = new();
    public int Round { get; set; } = 0;
    public string BennyRank { get; set; } = "A";
    public string BennyRankName => BennyRank;
    public int TurnIndex { get; set; } = 0;
    public DateTime TurnStart { get; set; } = DateTime.UtcNow;
    public bool RoundOver { get; set; } = false;
    public string? RoundWinnerName { get; set; }
    public bool GameOver { get; set; } = false;
    public string? GameWinnerName { get; set; }
    public int MaxRounds { get; set; } = 13;
    public List<string> Messages { get; } = new();
    public object Lock { get; } = new();

    public BennyLobby(string id) { Id = id; }

    public void AddMessage(string msg) { lock (Lock) { Messages.Add(msg); if (Messages.Count > 100) Messages.RemoveAt(0); } }
    public string CurrentTurnPlayerName => Players.Count > 0 && TurnIndex < Players.Count ? Players[TurnIndex].Name : "";
    public bool IsPlayerTurn(string pid) => !RoundOver && Players.Count > 0 && TurnIndex < Players.Count && Players[TurnIndex].Id == pid;
    public string GetPhaseForPlayer(string pid) => IsPlayerTurn(pid) ? (Players[TurnIndex].HasDrawn ? "play" : "draw") : "wait";
    public int GetTurnSecondsRemaining() => Math.Max(0, 60 - (int)(DateTime.UtcNow - TurnStart).TotalSeconds);

    public void StartRound()
    {
        Round++;
        BennyRank = Ranks[(Round - 1) % 13];
        Deck = BuildShuffledDeck();
        DiscardPile.Clear();
        RoundOver = false;
        RoundWinnerName = null;
        foreach (var p in Players) { p.Hand.Clear(); p.Melds.Clear(); p.HasDrawn = false; p.LastDrawnCardId = null; }
        for (int i = 0; i < 7; i++) foreach (var p in Players) { p.Hand.Add(Deck[0]); Deck.RemoveAt(0); }
        DiscardPile.Add(Deck[0]); Deck.RemoveAt(0);
        TurnIndex = (Round - 1) % Players.Count;
        TurnStart = DateTime.UtcNow;
        AddMessage($"Round {Round} started! Benny is {BennyRank}");
    }

    public void NextTurn()
    {
        lock (Lock)
        {
            Players[TurnIndex].HasDrawn = false;
            Players[TurnIndex].LastDrawnCardId = null;
            TurnIndex = (TurnIndex + 1) % Players.Count;
            TurnStart = DateTime.UtcNow;
            if (Deck.Count == 0 && DiscardPile.Count <= 1) { EndRound(null); }
        }
    }

    public void CheckTurnTimeout()
    {
        if (RoundOver || Players.Count == 0) return;
        if ((DateTime.UtcNow - TurnStart).TotalSeconds < 60) return;
        lock (Lock)
        {
            var p = Players[TurnIndex];
            if (!p.HasDrawn && Deck.Count > 0) { p.Hand.Add(Deck[0]); Deck.RemoveAt(0); p.HasDrawn = true; }
            if (p.Hand.Count > 0)
            {
                // Auto-discard highest value non-benny
                var discard = p.Hand.Where(c => c.Rank != BennyRank).OrderByDescending(c => CardValue(c)).FirstOrDefault() ?? p.Hand[0];
                p.Hand.Remove(discard); DiscardPile.Add(discard);
                AddMessage($"{p.Name} timed out, auto-discarded {discard.Rank}{discard.Suit}");
            }
            if (p.Hand.Count == 0) EndRound(p); else NextTurn();
        }
    }

    public void EndRound(BennyPlayer? winner)
    {
        lock (Lock)
        {
            RoundOver = true;
            RoundWinnerName = winner?.Name ?? "Nobody";
            foreach (var p in Players) { var penalty = p.Hand.Sum(c => CardValue(c)); p.Score += penalty; }
            AddMessage($"Round {Round} over! {RoundWinnerName} went out.");
            foreach (var p in Players) AddMessage($"  {p.Name}: +{p.Hand.Sum(c => CardValue(c))} pts (total: {p.Score})");
            if (Round >= MaxRounds)
            {
                GameOver = true;
                GameWinnerName = Players.OrderBy(p => p.Score).First().Name;
                AddMessage($"Game Over! {GameWinnerName} wins with {Players.Min(p => p.Score)} points!");
            }
        }
    }

    int CardValue(BennyCard c) => c.Rank == BennyRank ? 20 : (RankVal.TryGetValue(c.Rank, out var v) ? v : 10);

    public static bool IsValidMeld(List<BennyCard> cards, string bennyRank)
    {
        if (cards.Count < 3) return false;
        var nonBenny = cards.Where(c => c.Rank != bennyRank).ToList();
        int bennys = cards.Count - nonBenny.Count;

        // All bennys — valid set if ≤4
        if (nonBenny.Count == 0) return cards.Count <= 4;

        // Try set (same rank, different suits)
        var firstRank = nonBenny[0].Rank;
        if (nonBenny.All(c => c.Rank == firstRank) && cards.Count <= 4)
        {
            var suits = new HashSet<string>(nonBenny.Select(c => c.Suit));
            if (suits.Count == nonBenny.Count) return true;
        }

        // Try run (same suit, consecutive)
        var firstSuit = nonBenny[0].Suit;
        if (!nonBenny.All(c => c.Suit == firstSuit)) return false;
        var indices = nonBenny.Select(c => Array.IndexOf(Ranks, c.Rank)).OrderBy(x => x).ToList();
        int span = indices.Last() - indices.First() + 1;
        int needed = 0;
        for (int i = 1; i < indices.Count; i++) needed += (indices[i] - indices[i - 1] - 1);
        return span == cards.Count && needed <= bennys;
    }

    List<BennyCard> BuildShuffledDeck()
    {
        var deck = new List<BennyCard>();
        foreach (var s in Suits) foreach (var r in Ranks) deck.Add(new BennyCard { Rank = r, Suit = s });
        for (int i = deck.Count - 1; i > 0; i--) { int j = Random.Shared.Next(i + 1); (deck[i], deck[j]) = (deck[j], deck[i]); }
        return deck;
    }
}
