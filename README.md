# Hambargness 🎴

A fun ASP.NET Core 9 web application featuring two distinct experiences: an entertaining solitaire-themed card physics simulator and a multiplayer rummy card game.

## Features

### 1. Hambargness - Solitaire Win Screen 🃏

An interactive card physics simulator where you can select famous tech billionaires and watch their portrait cards fall and bounce on a canvas with realistic gravity and physics.

**Features:**
- Select from 7 famous tech personalities: Satya Nadella, Bill Gates, Jeff Bezos, Steve Jobs, Steve Ballmer, Larry Ellison, and Elon Musk
- Light/dark theme toggle
- Realistic card physics with bounce and friction
- Profile image proxying to avoid CORS issues
- In-memory image caching for performance

**Access:** Navigate to the root URL `/`

### 2. Benny - Multiplayer Rummy Card Game 🎲

A wild-card rummy variant supporting 2-6 players per lobby with real-time gameplay and JWT-based authentication.

**Game Features:**
- **Configurable rounds**: 1-13 rounds per game
- **Benny wild cards**: One rank per round serves as a wild card (worth 20 points)
- **Classic rummy mechanics**: Draw, meld, lay off, and discard
- **Multiplayer lobbies**: Create or join game lobbies
- **Real-time updates**: Poll-based game state synchronization

**Access:** Navigate to `/benny`

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation

1. Clone the repository:
   ```bash
   git clone https://github.com/WillEastbury/Hambargness.git
   cd Hambargness
   ```

2. Navigate to the project directory:
   ```bash
   cd Hambargness
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

4. Open your browser and navigate to:
   - `http://localhost:5000` or `https://localhost:5001` (or the port shown in your console)

## Project Structure

```
Hambargness/
├── Hambargness/
│   ├── Program.cs              # Main application with all game logic
│   ├── Hambargness.csproj      # Project configuration
│   ├── appsettings.json        # Application settings
│   └── appsettings.Development.json
└── README.md
```

## API Endpoints

### Hambargness Card Physics

- `GET /` - Main Hambargness interface
- `GET /img/{name}` - Proxy endpoint for billionaire profile images

### Benny Rummy Game

- `GET /benny` - Benny game interface
- `GET /benny/api/lobbies` - List all active lobbies
- `GET /benny/api/join/{lobbyId}/{playerName}` - Join a lobby (returns JWT token)
- `POST /benny/api/start` - Start the game (requires 2+ players)
- `POST /benny/api/rounds/{count}` - Set the number of rounds (1-13)
- `GET /benny/api/state` - Get current game state
- `POST /benny/api/draw/deck` - Draw a card from the deck
- `POST /benny/api/draw/discard` - Draw a card from the discard pile
- `POST /benny/api/meld` - Lay down a meld (3+ cards)
- `POST /benny/api/discard/{cardId}` - Discard a card
- `POST /benny/api/layoff/{playerName}/{meldIndex}` - Lay off cards onto an existing meld
- `POST /benny/api/nextround` - Proceed to the next round

## Technologies Used

- **ASP.NET Core 9** - Web framework with minimal APIs
- **.NET 9** - Runtime platform
- **JWT Authentication** - Secure player authentication (`System.IdentityModel.Tokens.Jwt`)
- **HTML5 Canvas** - Card physics rendering
- **Vanilla JavaScript** - Frontend interactivity
- **HttpClient** - Image proxying

## Game Rules - Benny Rummy

1. **Objective**: Be the first to discard all cards by forming valid melds
2. **Setup**: Each player receives 10 cards, with one card face-up in the discard pile
3. **Benny Wild Cards**: Each round, a specific rank acts as a wild card (worth 20 points)
4. **Turn Phases**:
   - **Draw**: Pick from the deck or discard pile
   - **Play**: Optionally lay melds (3+ cards in sequence or set) or lay off cards onto existing melds
   - **Discard**: Place one card on the discard pile
5. **Winning**: First player to empty their hand wins the round
6. **Scoring**: Points based on cards remaining in opponents' hands

## Development

### Build the Project

```bash
dotnet build
```

### Run in Development Mode

```bash
dotnet run --environment Development
```

### Publish for Production

```bash
dotnet publish -c Release -o ./publish
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is open source and available under the terms specified by the repository owner.

## Acknowledgments

- Card game mechanics inspired by classic rummy variants
- Physics simulation for an entertaining visual experience
- Built with ❤️ using ASP.NET Core
