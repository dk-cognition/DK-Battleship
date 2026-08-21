using DKBattleship.Core;
using DKBattleship.Core.Ai;
using Xunit;

namespace DKBattleship.Tests;

public class GameTests
{
    private static Game NewGame(int seed = 99) => new(GolfCharacters.TigerWoods, random: new Random(seed));

    [Fact]
    public void NewGame_StartsInPlacingPhaseWithAiBoardReady()
    {
        var game = NewGame();

        Assert.Equal(GameStatus.Placing, game.Status);
        Assert.Equal(5, game.ShipsToPlace.Count);
        Assert.Equal("Driver", game.NextShipToPlace!.Name);
        Assert.Equal(5, game.AiBoard.Ships.Count);
        Assert.All(game.AiBoard.Ships, s => Assert.True(s.IsPlaced));
        Assert.Empty(game.PlayerBoard.Ships);
    }

    [Fact]
    public void PlacePlayerShip_RejectsInvalidLieAndKeepsQueue()
    {
        var game = NewGame();

        Assert.False(game.PlacePlayerShip(new Coordinate(0, 8), Orientation.Horizontal));
        Assert.Equal(5, game.ShipsToPlace.Count);
        Assert.Equal(GameStatus.Placing, game.Status);
    }

    [Fact]
    public void PlacingAllShips_StartsTheBattle()
    {
        var game = NewGame();

        for (var row = 0; row < 5; row++)
        {
            Assert.True(game.PlacePlayerShip(new Coordinate(row * 2, 0), Orientation.Horizontal));
        }

        Assert.Empty(game.ShipsToPlace);
        Assert.Equal(GameStatus.PlayerTurn, game.Status);
        Assert.Equal(5, game.PlayerBoard.Ships.Count);
    }

    [Fact]
    public void RandomizePlayerShips_PlacesWholeBagAndStartsBattle()
    {
        var game = NewGame();
        game.RandomizePlayerShips();

        Assert.Equal(5, game.PlayerBoard.Ships.Count);
        Assert.Equal(GameStatus.PlayerTurn, game.Status);
        Assert.Equal(new[] { 5, 4, 3, 3, 2 }, game.PlayerBoard.Ships.Select(s => s.Size));
    }

    [Fact]
    public void PlayerFire_IsIgnoredDuringPlacement()
    {
        var game = NewGame();
        Assert.Equal(ShotResult.AlreadyShot, game.PlayerFire(new Coordinate(0, 0)));
        Assert.Equal(GameStatus.Placing, game.Status);
        Assert.Equal(0, game.PlayerSwings);
    }

    [Fact]
    public void PlayerFire_PassesTurnToAi_AndRepeatShotDoesNot()
    {
        var game = NewGame();
        game.RandomizePlayerShips();

        var target = new Coordinate(0, 0);
        game.PlayerFire(target);
        Assert.Equal(GameStatus.AiTurn, game.Status);
        Assert.Equal(1, game.PlayerSwings);

        game.AiFire();
        Assert.Equal(GameStatus.PlayerTurn, game.Status);

        Assert.Equal(ShotResult.AlreadyShot, game.PlayerFire(target));
        Assert.Equal(GameStatus.PlayerTurn, game.Status);
        Assert.Equal(1, game.PlayerSwings);
    }

    [Fact]
    public void AiFire_ThrowsWhenNotItsTurn()
    {
        var game = NewGame();
        game.RandomizePlayerShips();
        Assert.Throws<InvalidOperationException>(() => game.AiFire());
    }

    [Fact]
    public void PlayerWins_WhenEveryAiShipIsSunk()
    {
        var game = NewGame();
        game.RandomizePlayerShips();

        var targets = game.AiBoard.Ships.SelectMany(s => s.Coordinates).Distinct().ToList();
        foreach (var target in targets)
        {
            if (game.IsOver)
            {
                break;
            }

            game.PlayerFire(target);
            if (game.Status == GameStatus.AiTurn)
            {
                game.AiFire();
            }
        }

        Assert.Equal(GameStatus.PlayerWon, game.Status);
        Assert.True(game.IsOver);
        Assert.Contains("Winner", game.StatusMessage);
    }

    [Fact]
    public void PlayerCannotFireAfterGameOver()
    {
        var game = NewGame();
        game.RandomizePlayerShips();

        foreach (var target in game.AiBoard.Ships.SelectMany(s => s.Coordinates).Distinct().ToList())
        {
            if (game.IsOver)
            {
                break;
            }

            game.PlayerFire(target);
            if (game.Status == GameStatus.AiTurn)
            {
                game.AiFire();
            }
        }

        Assert.Equal(GameStatus.PlayerWon, game.Status);
        var swings = game.PlayerSwings;
        Assert.Equal(ShotResult.AlreadyShot, game.PlayerFire(new Coordinate(0, 0)));
        Assert.Equal(swings, game.PlayerSwings);
    }

    [Fact]
    public void AiWins_WhenItClearsThePlayersBag()
    {
        var game = NewGame(5);
        game.RandomizePlayerShips();
        var guard = 0;

        while (!game.IsOver && guard++ < 500)
        {
            // Player deliberately plays the same corner-safe pattern; AI should still finish the job.
            if (game.Status == GameStatus.PlayerTurn)
            {
                var untried = Enumerable.Range(0, 10)
                    .SelectMany(r => Enumerable.Range(0, 10).Select(c => new Coordinate(r, c)))
                    .First(c => game.AiBoard[c] is CellState.Empty or CellState.Ship);
                game.PlayerFire(untried);
            }

            if (game.Status == GameStatus.AiTurn)
            {
                game.AiFire();
            }
        }

        Assert.True(game.IsOver);
    }

    [Fact]
    public void AiNeverRepeatsAShotAcrossAFullGame()
    {
        var game = NewGame(21);
        game.RandomizePlayerShips();
        var aiShots = new HashSet<Coordinate>();
        var guard = 0;

        while (!game.IsOver && guard++ < 500)
        {
            if (game.Status == GameStatus.PlayerTurn)
            {
                var untried = Enumerable.Range(0, 10)
                    .SelectMany(r => Enumerable.Range(0, 10).Select(c => new Coordinate(r, c)))
                    .First(c => game.AiBoard[c] is CellState.Empty or CellState.Ship);
                game.PlayerFire(untried);
            }

            if (game.Status == GameStatus.AiTurn)
            {
                var shot = game.AiFire();
                Assert.NotEqual(ShotResult.AlreadyShot, shot.Result);
                Assert.True(aiShots.Add(shot.Coordinate), $"AI repeated {shot.Coordinate}");
            }
        }

        Assert.True(game.IsOver);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(42)]
    [InlineData(99)]
    [InlineData(123)]
    [InlineData(2026)]
    public void TigerWoods_ClearsThePlayersBagInAtMostFortySwings(int seed)
    {
        var game = new Game(GolfCharacters.TigerWoods, random: new Random(seed));
        game.RandomizePlayerShips();
        var guard = 0;

        while (!game.IsOver && guard++ < 500)
        {
            if (game.Status == GameStatus.PlayerTurn)
            {
                // A weak player sweeping in reading order, so Tiger has to do the closing.
                var untried = Enumerable.Range(0, 10)
                    .SelectMany(r => Enumerable.Range(0, 10).Select(c => new Coordinate(r, c)))
                    .First(c => game.AiBoard[c] is CellState.Empty or CellState.Ship);
                game.PlayerFire(untried);
            }

            if (game.Status == GameStatus.AiTurn)
            {
                game.AiFire();
            }
        }

        Assert.Equal(GameStatus.AiWon, game.Status);
        Assert.True(game.AiSwings <= 40, $"Tiger needed {game.AiSwings} swings (seed {seed})");
    }

    [Fact]
    public void Characters_SeedRosterExposesDistinctStrategies()
    {
        Assert.Equal(4, GolfCharacters.All.Count);
        var random = new Random(1);
        var strategies = GolfCharacters.All.Select(c => c.CreateStrategy(random)).ToList();

        Assert.Equal(
            new[] { "Tiger Woods", "Jordan Spieth", "Jackson Koivun", "Kyle Stalder" },
            strategies.Select(s => s.Name));
        Assert.All(GolfCharacters.All, c => Assert.False(string.IsNullOrWhiteSpace(c.Description)));
    }

    [Fact]
    public void Game_UsesTheSelectedCharactersStrategy()
    {
        var game = new Game(GolfCharacters.KyleStalder, random: new Random(4));
        Assert.Equal("Kyle Stalder", game.Ai.Name);
        Assert.Equal("Kyle Stalder", game.Opponent.Name);
    }
}
