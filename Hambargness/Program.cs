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

app.Run();
