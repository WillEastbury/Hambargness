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

app.MapGet("/benny", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
<title>Benny - Card Game</title>
<meta name="viewport" content="width=device-width, initial-scale=1">
<style>
*{margin:0;padding:0;box-sizing:border-box}
body{background:#1a6d1a;font-family:'Segoe UI',system-ui,sans-serif;color:#fff;min-height:100vh;display:flex;flex-direction:column;align-items:center;overflow-x:hidden}
.top-bar{width:100%;background:rgba(0,0,0,0.3);padding:10px 20px;display:flex;justify-content:space-between;align-items:center;font-size:14px}
.top-bar a{color:#feca57;text-decoration:none;font-weight:600}
.top-bar a:hover{text-decoration:underline}
.info-bar{display:flex;gap:20px;align-items:center;flex-wrap:wrap}
.info-bar .badge{background:rgba(255,255,255,0.15);padding:4px 12px;border-radius:20px;font-size:13px}
.benny-badge{background:#feca57!important;color:#333!important;font-weight:700}
.game-area{flex:1;display:flex;flex-direction:column;align-items:center;width:100%;max-width:900px;padding:10px}
.zone{margin:8px 0;text-align:center}
.zone-label{font-size:11px;text-transform:uppercase;letter-spacing:1px;opacity:0.6;margin-bottom:4px}
.card-row{display:flex;gap:6px;justify-content:center;flex-wrap:wrap;min-height:100px}
.card{width:70px;height:100px;border-radius:8px;display:flex;flex-direction:column;align-items:center;justify-content:center;font-weight:700;cursor:default;transition:all 0.15s;position:relative;box-shadow:0 2px 8px rgba(0,0,0,0.3);border:2px solid transparent;flex-shrink:0}
.card-white{background:#fff;color:#333}
.card-red{background:#fff;color:#cc0000}
.card-back{background:linear-gradient(135deg,#1a3a8a,#2d5dc7);color:#fff;cursor:pointer}
.card-back:hover{transform:translateY(-3px);box-shadow:0 6px 16px rgba(0,0,0,0.4)}
.card.selected{border-color:#feca57;transform:translateY(-8px);box-shadow:0 8px 20px rgba(254,202,87,0.4)}
.card.is-benny{box-shadow:0 0 12px 3px #feca57}
.card .rank{font-size:22px;line-height:1}
.card .suit{font-size:18px;line-height:1}
.card .corner{position:absolute;top:3px;left:5px;font-size:10px;line-height:1.1;text-align:left}
.card .corner-br{position:absolute;bottom:3px;right:5px;font-size:10px;line-height:1.1;text-align:right;transform:rotate(180deg)}
.discard-zone{display:flex;gap:10px;align-items:center;justify-content:center}
.pile{width:70px;height:100px;border-radius:8px;border:2px dashed rgba(255,255,255,0.3);display:flex;align-items:center;justify-content:center;font-size:11px;opacity:0.5}
.actions{display:flex;gap:8px;margin:8px 0;flex-wrap:wrap;justify-content:center}
.btn{padding:8px 18px;border:none;border-radius:8px;font-size:14px;font-weight:600;cursor:pointer;transition:all 0.2s}
.btn-primary{background:#feca57;color:#333}
.btn-primary:hover{background:#f9b824}
.btn-danger{background:#e74c3c;color:#fff}
.btn-danger:hover{background:#c0392b}
.btn:disabled{opacity:0.3;cursor:not-allowed}
.msg{background:rgba(0,0,0,0.4);padding:8px 16px;border-radius:8px;margin:6px 0;font-size:14px;text-align:center;min-height:30px}
.scores{background:rgba(0,0,0,0.3);border-radius:10px;padding:12px 20px;margin:8px 0;width:100%;max-width:400px}
.scores h3{font-size:14px;margin-bottom:6px;text-align:center}
.scores table{width:100%;font-size:13px;border-collapse:collapse}
.scores td{padding:3px 8px}
.scores .you{color:#feca57;font-weight:700}
.meld-zone{display:flex;gap:12px;flex-wrap:wrap;justify-content:center;min-height:60px}
.meld-group{display:flex;gap:2px;padding:4px;background:rgba(255,255,255,0.08);border-radius:6px}
.meld-group .card{width:50px;height:72px;font-size:11px;cursor:default}
.meld-group .card .rank{font-size:16px}
.meld-group .card .suit{font-size:14px}
.meld-group .card .corner,.meld-group .card .corner-br{display:none}
.overlay{position:fixed;inset:0;background:rgba(0,0,0,0.7);display:flex;align-items:center;justify-content:center;z-index:100}
.overlay-box{background:#2d5a2d;border-radius:16px;padding:30px 40px;text-align:center;box-shadow:0 8px 32px rgba(0,0,0,0.5);max-width:400px}
.overlay-box h2{font-size:24px;margin-bottom:12px}
.overlay-box p{margin-bottom:16px;opacity:0.8}
</style>
</head>
<body>
<div class="top-bar">
    <a href="/">&larr; Back to Hambargness</a>
    <div class="info-bar">
        <span class="badge" id="round-badge">Round 1/13</span>
        <span class="badge benny-badge" id="benny-badge">Benny: A</span>
        <span class="badge" id="turn-badge">Your Turn</span>
    </div>
</div>

<div class="game-area">
    <div class="zone">
        <div class="zone-label">Opponent's Hand</div>
        <div class="card-row" id="opp-hand"></div>
    </div>

    <div class="zone">
        <div class="zone-label">Opponent's Melds</div>
        <div class="meld-zone" id="opp-melds"></div>
    </div>

    <div class="zone">
        <div class="zone-label">Table</div>
        <div class="discard-zone">
            <div id="draw-pile" class="card card-back" onclick="drawFromDeck()" title="Draw from deck">
                <span style="font-size:11px">DRAW</span>
            </div>
            <div id="discard-top" class="pile">Empty</div>
        </div>
    </div>

    <div class="msg" id="msg">Draw a card to begin your turn.</div>
    <div class="actions">
        <button class="btn btn-primary" id="btn-draw-discard" onclick="drawFromDiscard()" disabled>Pick Up Discard</button>
        <button class="btn btn-primary" id="btn-meld" onclick="layMeld()" disabled>Lay Meld</button>
        <button class="btn btn-danger" id="btn-discard" onclick="discardSelected()" disabled>Discard</button>
    </div>

    <div class="zone">
        <div class="zone-label">Your Melds</div>
        <div class="meld-zone" id="my-melds"></div>
    </div>

    <div class="zone">
        <div class="zone-label">Your Hand (click to select)</div>
        <div class="card-row" id="my-hand"></div>
    </div>

    <div class="scores" id="scoreboard">
        <h3>Scores</h3>
        <table><tr><td class="you">You</td><td class="you" id="score-you">0</td></tr><tr><td>Opponent</td><td id="score-opp">0</td></tr></table>
    </div>
</div>

<div class="overlay" id="overlay" style="display:none">
    <div class="overlay-box">
        <h2 id="overlay-title">Round Over!</h2>
        <p id="overlay-msg"></p>
        <button class="btn btn-primary" onclick="nextRound()">Next Round</button>
    </div>
</div>

<script>
const SUITS = ['♠','♥','♦','♣'];
const RANKS = ['A','2','3','4','5','6','7','8','9','10','J','Q','K'];
const RANK_VAL = {A:1,'2':2,'3':3,'4':4,'5':5,'6':6,'7':7,'8':8,'9':9,'10':10,J:10,Q:10,K:10};
const BENNY_PENALTY = 20;
const FACE_PENALTY = 10;

let deck=[], myHand=[], oppHand=[], discardPile=[], myMelds=[], oppMelds=[];
let round=0, bennyRank='', myScore=0, oppScore=0;
let phase='draw'; // draw, play, opp
let selected = new Set();

function makeCard(rank,suit){ return {rank,suit,id:rank+suit}; }
function isRed(c){ return c.suit==='♥'||c.suit==='♦'; }
function isBenny(c){ return c.rank===bennyRank; }
function cardVal(c){ if(isBenny(c)) return BENNY_PENALTY; if('JQK'.includes(c.rank)) return FACE_PENALTY; return RANK_VAL[c.rank]; }
function rankIdx(r){ return RANKS.indexOf(r); }

function buildDeck(){
    let d=[];
    for(const s of SUITS) for(const r of RANKS) d.push(makeCard(r,s));
    return d;
}
function shuffle(a){ for(let i=a.length-1;i>0;i--){ const j=Math.floor(Math.random()*(i+1));[a[i],a[j]]=[a[j],a[i]]; } return a; }

function startRound(){
    bennyRank = RANKS[round % 13];
    deck = shuffle(buildDeck());
    myHand=[]; oppHand=[]; discardPile=[]; myMelds=[]; oppMelds=[];
    selected.clear();
    for(let i=0;i<7;i++){ myHand.push(deck.pop()); oppHand.push(deck.pop()); }
    discardPile.push(deck.pop());
    phase='draw';
    updateUI();
    setMsg('Draw a card from the deck or pick up the discard.');
}

function setMsg(t){ document.getElementById('msg').textContent=t; }

function renderCard(c, small){
    const col = isRed(c)?'card-red':'card-white';
    const bClass = isBenny(c)?'is-benny':'';
    return `<span class="rank">${c.rank}</span><span class="suit">${c.suit}</span>`
        +`<span class="corner">${c.rank}<br>${c.suit}</span><span class="corner-br">${c.rank}<br>${c.suit}</span>`;
}

function updateUI(){
    // Round info
    document.getElementById('round-badge').textContent = `Round ${round+1}/13`;
    document.getElementById('benny-badge').textContent = `Benny: ${bennyRank}`;
    document.getElementById('turn-badge').textContent = phase==='opp'?'Opponent\'s Turn':'Your Turn';

    // Opponent hand (face down)
    document.getElementById('opp-hand').innerHTML = oppHand.map(()=>'<div class="card card-back" style="cursor:default"><span style="font-size:11px">?</span></div>').join('');

    // Discard
    const dt = document.getElementById('discard-top');
    if(discardPile.length){
        const top = discardPile[discardPile.length-1];
        const col = isRed(top)?'card-red':'card-white';
        const bClass = isBenny(top)?'is-benny':'';
        dt.className = `card ${col} ${bClass}`;
        dt.innerHTML = renderCard(top);
        dt.style.cursor='default';
    } else {
        dt.className='pile'; dt.innerHTML='Empty';
    }

    // Draw pile
    document.getElementById('draw-pile').style.display = deck.length?'flex':'none';

    // My hand
    const mh = document.getElementById('my-hand');
    myHand.sort((a,b)=>{ const si=SUITS.indexOf(a.suit)-SUITS.indexOf(b.suit); if(si!==0) return si; return rankIdx(a.rank)-rankIdx(b.rank); });
    mh.innerHTML = myHand.map((c,i)=>{
        const col = isRed(c)?'card-red':'card-white';
        const sel = selected.has(i)?'selected':'';
        const bClass = isBenny(c)?'is-benny':'';
        return `<div class="card ${col} ${sel} ${bClass}" onclick="toggleSelect(${i})">${renderCard(c)}</div>`;
    }).join('');

    // Melds
    renderMelds('my-melds', myMelds);
    renderMelds('opp-melds', oppMelds);

    // Buttons
    document.getElementById('btn-draw-discard').disabled = phase!=='draw' || !discardPile.length;
    document.getElementById('btn-meld').disabled = phase!=='play';
    document.getElementById('btn-discard').disabled = phase!=='play' || selected.size!==1;

    // Scores
    document.getElementById('score-you').textContent = myScore;
    document.getElementById('score-opp').textContent = oppScore;
}

function renderMelds(elId, melds){
    const el = document.getElementById(elId);
    if(!melds.length){ el.innerHTML='<span style="opacity:0.3;font-size:12px">No melds yet</span>'; return; }
    el.innerHTML = melds.map(m=>'<div class="meld-group">'+m.map(c=>{
        const col=isRed(c)?'card-red':'card-white';
        const bClass=isBenny(c)?'is-benny':'';
        return `<div class="card ${col} ${bClass}" style="width:50px;height:72px">${renderCard(c,true)}</div>`;
    }).join('')+'</div>').join('');
}

function toggleSelect(i){
    if(phase!=='play') return;
    selected.has(i)?selected.delete(i):selected.add(i);
    updateUI();
}

function drawFromDeck(){
    if(phase!=='draw'||!deck.length) return;
    myHand.push(deck.pop());
    phase='play';
    selected.clear();
    setMsg('Select cards to meld or select one card to discard.');
    updateUI();
}

function drawFromDiscard(){
    if(phase!=='draw'||!discardPile.length) return;
    myHand.push(discardPile.pop());
    phase='play';
    selected.clear();
    setMsg('Select cards to meld or select one card to discard.');
    updateUI();
}

// Validate a meld: set of 3-4 same rank, or run of 3+ same suit (bennys fill gaps)
function isValidMeld(cards){
    if(cards.length<3) return false;
    const nonBenny = cards.filter(c=>!isBenny(c));
    const bennys = cards.length - nonBenny.length;

    // Try set (same rank)
    if(nonBenny.length>0){
        const rank = nonBenny[0].rank;
        if(nonBenny.every(c=>c.rank===rank) && cards.length<=4){
            const suits = new Set(nonBenny.map(c=>c.suit));
            if(suits.size===nonBenny.length) return true;
        }
    } else if(cards.length<=4) return true; // all bennys, treat as set

    // Try run (same suit, consecutive)
    if(nonBenny.length>0){
        const suit = nonBenny[0].suit;
        if(!nonBenny.every(c=>c.suit===suit)) return false;
        const indices = nonBenny.map(c=>rankIdx(c.rank)).sort((a,b)=>a-b);
        let needed = 0;
        for(let i=1;i<indices.length;i++) needed += (indices[i]-indices[i-1]-1);
        const totalSpan = indices[indices.length-1]-indices[0]+1;
        if(totalSpan===cards.length && needed<=bennys) return true;
    }
    return false;
}

function layMeld(){
    if(phase!=='play') return;
    const selCards = [...selected].sort((a,b)=>a-b).map(i=>myHand[i]);
    if(!isValidMeld(selCards)){ setMsg('Not a valid meld! Need 3+ of same rank, or 3+ run in same suit.'); return; }
    myMelds.push(selCards);
    const toRemove = [...selected].sort((a,b)=>b-a);
    toRemove.forEach(i=>myHand.splice(i,1));
    selected.clear();
    if(myHand.length===0){ endRound('you'); return; }
    setMsg('Meld laid! Select more or discard to end turn.');
    updateUI();
}

function discardSelected(){
    if(phase!=='play'||selected.size!==1) return;
    const idx = [...selected][0];
    discardPile.push(myHand.splice(idx,1)[0]);
    selected.clear();
    if(myHand.length===0){ endRound('you'); return; }
    phase='opp';
    updateUI();
    setMsg('Opponent is thinking...');
    setTimeout(oppTurn, 800);
}

// Simple AI
function oppTurn(){
    // Draw
    if(discardPile.length && Math.random()<0.4){
        oppHand.push(discardPile.pop());
    } else if(deck.length){
        oppHand.push(deck.pop());
    } else if(discardPile.length){
        oppHand.push(discardPile.pop());
    }

    // Try to find melds
    let found = true;
    while(found){
        found=false;
        // Try sets
        const byRank={};
        oppHand.forEach((c,i)=>{ const k=isBenny(c)?'__benny__':c.rank; if(!byRank[k]) byRank[k]=[]; byRank[k].push(i); });
        const bennyIdxs = byRank['__benny__']||[];
        for(const r of RANKS){
            if(r===bennyRank) continue;
            const group = byRank[r]||[];
            const total = group.length + bennyIdxs.length;
            if(total>=3 && group.length>=2){
                const take = group.slice(0,Math.min(group.length,4));
                let need = Math.max(0, 3-take.length);
                const bTake = bennyIdxs.slice(0,need);
                const meld = [...take,...bTake].map(i=>oppHand[i]);
                if(isValidMeld(meld)){
                    const removeIdxs = [...take,...bTake].sort((a,b)=>b-a);
                    removeIdxs.forEach(i=>oppHand.splice(i,1));
                    oppMelds.push(meld);
                    found=true; break;
                }
            }
        }
        if(found) continue;
        // Try runs
        for(const s of SUITS){
            const inSuit = oppHand.map((c,i)=>({c,i})).filter(x=>!isBenny(x.c)&&x.c.suit===s).sort((a,b)=>rankIdx(a.c.rank)-rankIdx(b.c.rank));
            for(let start=0;start<inSuit.length-1;start++){
                for(let end=start+1;end<inSuit.length;end++){
                    const span = rankIdx(inSuit[end].c.rank)-rankIdx(inSuit[start].c.rank)+1;
                    const have = end-start+1;
                    const gaps = span-have;
                    if(span>=3 && gaps<=bennyIdxs.length){
                        const take = inSuit.slice(start,end+1).map(x=>x.i);
                        const bTake = bennyIdxs.slice(0,gaps);
                        const meld = [...take,...bTake].map(i=>oppHand[i]);
                        if(isValidMeld(meld)){
                            const removeIdxs = [...take,...bTake].sort((a,b)=>b-a);
                            removeIdxs.forEach(i=>oppHand.splice(i,1));
                            oppMelds.push(meld);
                            found=true; break;
                        }
                    }
                }
                if(found) break;
            }
            if(found) break;
        }
    }

    if(oppHand.length===0){ endRound('opp'); return; }

    // Discard highest value non-benny card
    let discIdx=0, maxVal=0;
    oppHand.forEach((c,i)=>{ if(!isBenny(c)){ const v=cardVal(c); if(v>=maxVal){maxVal=v;discIdx=i;} }});
    if(oppHand.length>0){
        discardPile.push(oppHand.splice(discIdx,1)[0]);
    }

    if(oppHand.length===0){ endRound('opp'); return; }
    if(!deck.length && !discardPile.length){ endRound('draw'); return; }

    phase='draw';
    setMsg('Your turn! Draw a card.');
    updateUI();
}

function endRound(winner){
    let myPenalty=0, oppPenalty=0;
    myHand.forEach(c=>myPenalty+=cardVal(c));
    oppHand.forEach(c=>oppPenalty+=cardVal(c));
    myScore+=myPenalty; oppScore+=oppPenalty;

    const title = winner==='you'?'You Won the Round!':winner==='opp'?'Opponent Won!':'Draw!';
    const msg = `Penalty points — You: +${myPenalty}, Opponent: +${oppPenalty}\nTotal — You: ${myScore}, Opponent: ${oppScore}`;
    document.getElementById('overlay-title').textContent=title;
    document.getElementById('overlay-msg').textContent=msg;
    document.getElementById('overlay').style.display='flex';
    updateUI();
}

function nextRound(){
    document.getElementById('overlay').style.display='none';
    round++;
    if(round>=13){
        const winner = myScore<oppScore?'You Win!':myScore>oppScore?'Opponent Wins!':'It\'s a Tie!';
        alert(`Game Over! Final — You: ${myScore}, Opponent: ${oppScore}. ${winner}`);
        round=0; myScore=0; oppScore=0;
    }
    startRound();
}

startRound();
</script>
</body>
</html>
""", "text/html"));

app.Run();
