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
let imgLoaded = false;

// Draw Satya onto an offscreen canvas as fallback-proof source
const img = new Image();
img.crossOrigin = 'anonymous';
img.onload = () => { imgLoaded = true; startAnimation(); };
img.onerror = () => {
    // Fallback: draw a card with text if image fails
    imgLoaded = false;
    startAnimation();
};
img.src = 'https://pbs.twimg.com/profile_images/1221837516816306177/_Ld4un5A_400x400.jpg';

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

    ctx.beginPath();
    ctx.roundRect(x + 4, y + 4, cardW - 8, cardH - 8, 4);
    ctx.clip();

    if (imgLoaded) {
        ctx.drawImage(img, x + 4, y + 4, cardW - 8, cardH - 8);
    } else {
        ctx.fillStyle = '#1a6fb5';
        ctx.fillRect(x + 4, y + 4, cardW - 8, cardH - 8);
        ctx.fillStyle = '#fff';
        ctx.font = 'bold 11px sans-serif';
        ctx.textAlign = 'center';
        ctx.fillText('SATYA', x + cardW/2, y + cardH/2 - 6);
        ctx.fillText('NADELLA', x + cardW/2, y + cardH/2 + 10);
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
