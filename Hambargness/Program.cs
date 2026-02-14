var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => Results.Content("""
<!DOCTYPE html>
<html>
<head>
    <title>Hambargness - Solitaire Win!</title>
    <style>
        * { margin: 0; padding: 0; }
        body { background: #008000; overflow: hidden; width: 100vw; height: 100vh; }
        canvas { display: block; }
    </style>
</head>
<body>
<canvas id="c"></canvas>
<script>
const canvas = document.getElementById('c');
const ctx = canvas.getContext('2d');
canvas.width = window.innerWidth;
canvas.height = window.innerHeight;

const cardW = 100;
const cardH = 130;
const cards = [];

const people = [
    { name: 'SATYA\nNADELLA', color: '#1a6fb5', loaded: false, img: new Image() },
    { name: 'BILL\nGATES', color: '#4a2d8a', loaded: false, img: new Image() },
    { name: 'JEFF\nBEZOS', color: '#c45500', loaded: false, img: new Image() }
];

let started = false;
function tryStart() {
    if (!started) { started = true; startAnimation(); }
}

people[0].img.crossOrigin = 'anonymous';
people[0].img.onload = () => { people[0].loaded = true; tryStart(); };
people[0].img.onerror = tryStart;
people[0].img.src = 'https://pbs.twimg.com/profile_images/1221837516816306177/_Ld4un5A_400x400.jpg';

people[1].img.crossOrigin = 'anonymous';
people[1].img.onload = () => { people[1].loaded = true; tryStart(); };
people[1].img.onerror = tryStart;
people[1].img.src = 'https://upload.wikimedia.org/wikipedia/commons/thumb/a/a8/Bill_Gates_2017_%28cropped%29.jpg/440px-Bill_Gates_2017_%28cropped%29.jpg';

people[2].img.crossOrigin = 'anonymous';
people[2].img.onload = () => { people[2].loaded = true; tryStart(); };
people[2].img.onerror = tryStart;
people[2].img.src = 'https://upload.wikimedia.org/wikipedia/commons/thumb/6/6d/Jeff_Bezos_at_Amazon_Spheres_Grand_Opening_in_Seattle_-_2018_%2839074799225%29_%28cropped%29.jpg/440px-Jeff_Bezos_at_Amazon_Spheres_Grand_Opening_in_Seattle_-_2018_%2839074799225%29_%28cropped%29.jpg';

// Start after 2s regardless
setTimeout(tryStart, 2000);

function startAnimation() {
    ctx.fillStyle = '#008000';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    setInterval(spawnCard, 120);
    update();
}

function spawnCard() {
    cards.push({
        x: Math.random() * (canvas.width - cardW),
        y: -cardH,
        vx: (Math.random() - 0.5) * 6,
        vy: Math.random() * 2 + 1,
        gravity: 0.15 + Math.random() * 0.1,
        bounced: false,
        person: Math.floor(Math.random() * 3)
    });
}

function drawCard(x, y, personIdx) {
    const p = people[personIdx];
    ctx.save();
    ctx.shadowColor = 'rgba(0,0,0,0.4)';
    ctx.shadowBlur = 6;
    ctx.shadowOffsetX = 3;
    ctx.shadowOffsetY = 3;

    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.roundRect(x, y, cardW, cardH, 6);
    ctx.fill();

    ctx.beginPath();
    ctx.roundRect(x + 4, y + 4, cardW - 8, cardH - 8, 4);
    ctx.clip();

    if (p.loaded) {
        ctx.drawImage(p.img, x + 4, y + 4, cardW - 8, cardH - 8);
    } else {
        ctx.fillStyle = p.color;
        ctx.fillRect(x + 4, y + 4, cardW - 8, cardH - 8);
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 11px sans-serif';
        ctx.textAlign = 'center';
        const lines = p.name.split('\n');
        ctx.fillText(lines[0], x + cardW/2, y + cardH/2 - 6);
        ctx.fillText(lines[1], x + cardW/2, y + cardH/2 + 10);
    }

    ctx.restore();
}

function update() {
    // Don't clear - that's the solitaire win effect! Cards leave trails
    for (const card of cards) {
        card.vy += card.gravity;
        card.x += card.vx;
        card.y += card.vy;

        // Bounce off bottom
        if (card.y + cardH > canvas.height) {
            card.y = canvas.height - cardH;
            card.vy = -card.vy * 0.6;
            card.vx *= 0.95;
            card.bounced = true;
        }

        // Bounce off sides
        if (card.x < 0) { card.x = 0; card.vx = -card.vx * 0.8; }
        if (card.x + cardW > canvas.width) { card.x = canvas.width - cardW; card.vx = -card.vx * 0.8; }

        drawCard(card.x, card.y, card.person);
    }

    // Remove cards that have settled
    for (let i = cards.length - 1; i >= 0; i--) {
        if (cards[i].bounced && Math.abs(cards[i].vy) < 0.5 && cards[i].y + cardH >= canvas.height - 1) {
            cards.splice(i, 1);
        }
    }

    requestAnimationFrame(update);
}

window.addEventListener('resize', () => {
    canvas.width = window.innerWidth;
    canvas.height = window.innerHeight;
    ctx.fillStyle = '#008000';
    ctx.fillRect(0, 0, canvas.width, canvas.height);
});
</script>
</body>
</html>
""", "text/html"));

app.Run();
