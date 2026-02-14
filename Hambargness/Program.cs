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

const img = new Image();
img.crossOrigin = 'anonymous';
img.src = 'https://upload.wikimedia.org/wikipedia/commons/thumb/7/78/MS-Exec-Nadella-Satya-2017-08-31-22_%28cropped%29.jpg/440px-MS-Exec-Nadella-Satya-2017-08-31-22_%28cropped%29.jpg';

const cardW = 100;
const cardH = 130;
const cards = [];

function spawnCard() {
    cards.push({
        x: Math.random() * (canvas.width - cardW),
        y: -cardH,
        vx: (Math.random() - 0.5) * 6,
        vy: Math.random() * 2 + 1,
        gravity: 0.15 + Math.random() * 0.1,
        bounced: false
    });
}

function drawCard(x, y) {
    ctx.save();
    ctx.shadowColor = 'rgba(0,0,0,0.4)';
    ctx.shadowBlur = 6;
    ctx.shadowOffsetX = 3;
    ctx.shadowOffsetY = 3;

    // White card border
    ctx.fillStyle = '#fff';
    ctx.beginPath();
    ctx.roundRect(x, y, cardW, cardH, 6);
    ctx.fill();

    // Draw Satya clipped into card
    ctx.beginPath();
    ctx.roundRect(x + 4, y + 4, cardW - 8, cardH - 8, 4);
    ctx.clip();
    ctx.drawImage(img, x + 4, y + 4, cardW - 8, cardH - 8);

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

        drawCard(card.x, card.y);
    }

    // Remove cards that have settled
    for (let i = cards.length - 1; i >= 0; i--) {
        if (cards[i].bounced && Math.abs(cards[i].vy) < 0.5 && cards[i].y + cardH >= canvas.height - 1) {
            cards.splice(i, 1);
        }
    }

    requestAnimationFrame(update);
}

img.onload = () => {
    // Fill background
    ctx.fillStyle = '#008000';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Spawn cards continuously
    setInterval(spawnCard, 120);
    update();
};

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
