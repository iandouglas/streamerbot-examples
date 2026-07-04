# Streamer.bot Game Ideas


## shared currency?

- (done) every xx seconds, award people some amount of points
    - (no) does this work if people aren't actively chatting?

- do we make a channel point reward to convert channel points to game points?

- players can run a !points command to see how many points they currently have
- !leaderboard will show players with top scores

## starting a game

- only streamer can use the !game command and will take two arguments, the name of the game, and number of games to play
- only one !game is active at a time


## joining a game

- for games that don't require the twitch prediction api, players will need to use a "!join" command otherwise their chat messages will be ignored
- once a game is running, all incoming messages will be analyzed through that game's parser, players who do not '!join' will have their message dropped/ignored



## prediction based games

### shell cup

- streamerbot will picka random number between 3 and 5
- it will show that many cups under which one marble is placed
- we'll show a random-looking animation of the cups moving around, but will be quite fast and dificult to visually follow
- streamerbot will run a prediction for 30 seconds with one item per prediction choice
- players will bet their channel points on the outcome
- streamerbot will remove each of the cups one at a time to reveal the winner
- streamerbot will 'resolve' the prediction poll to award channel points



## chat games

these will require players to !join so we know whose chat messages to watch or not


### whack-a-mole

inspired by @shindigs

- players join
- a board is drawn on the screen, 4 x 4 squares, all empty
- once game starts, a random number of players will be drawn to show up on the board
    - their names are only there for a few moments, the disappear again
- if someone types the coordinates (row/col) where a name appears:
    - that username in that coordinate gets 'whacked'
    - that username is timed out for 10 seconds
    - the user who typed in the coordinates wins 100 points
- if a player gets timed out 3 times, they're out of the game
- play continues until there's only one player remaining on the board
- player at the end wins 1000 bonus points



### mastermind

- starting the game will look like "!game mastermind 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin
    - players can !join to play that round until that 60 second timer ends, then no more players can join
- show 2 rows of 4 columns, the top row is empty, the bottom row looks 'hidden' for the solution
- above the board we'll show 6 to 8 twitch emotes, streamerbot will randomly pick which 4 will be used in the solution
    - an emote will only be used once in the solution
- players can send messages with 4 emotes, and if any of those match, the bot will send a reply to their chat message with emote colors letting them know if an emote matches somewhere in the solution or if the player has guessed the correct emote also in the correct position
    - each guess gets two scores: exact‑position matches vs right‑emoji‑wrong‑spot
    - the bot will respond with a "VoteYea" emote for a correct emote in the correct position
    - the bot will respond with a "TwitchSings" emote for a correct emote in the wrong position
    - the bot will respond with a "VoteNay" emote if the player's emote guess is incorrect
    - if the answer is "emote1 emote2 emote3 emote4":
        - if a player enters "emote4 emote7 emote3 emote1", bot will respond with "TwitchSings VoteNay Voteyea TwitchSings" since emote1 and emote 4 are part of the solution but in the wrong spot, emote3 is in the solution and in the correct spot and emote7 isn't in the solution at all.
- once all players have sent a message with the correct emote in the correct position, that emote will be revealed in the solution row
- the first player to send a chat message with the correct emotes in the correct order will win the game and immediately stop watching chat messages until the next round is started
- any other player who guessed an emote in the correct position will win a proportionate amount of points. (each emote is worth 250 points)
    - if the jackpot is 1000 points, the first player to send a chat message with the 4 emotes in the right order gets 1000 points
    - if a player guesses 2 emotes in the correct position they get 500 points
    - no points awarded for guessing a correct emote in the wrong position


### wheel of fortune

- we'll need a different name for this since we don't want to run into legal action, but we could do something like "phrase-spinner" or something
- starting the game will look like "!game phrase-spinner 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin
    - players can !join to play that round until that 60 second timer ends, then no more players can join
- the bot will randomize the order of the players when the round starts
- we'll show a QWERTY keyboard of the current letters to pick from
- we'll show a series of panels similar to a "wheel of fortune" solution with a blue background if that space is not needed or represents a space character, and a white space if a letter is needed; phrases will not contain numbers or punctuation
- the game will show a timer close for each player's turn

- the bot will 'spin' the wheel indicating point values from 10 points to 100 points in increments of 10
    - the wheel will have a few special segments:
        - "lose a turn" space immediately ends that player's turn if it lands on that spot
        - a "lose points" space which immediately zeros out that player's potential points for that round of the game
        - a "gift sub" space which will be reserved for that player only if they guess the correct solution; if another player guesses the solution, no gift subscription is given
- the bot will send a chat message mentioning the current player, and they will have 10 seconds to respond
    - the game will allow uppercase or lowercase letters
    - streamerbot will attend to a message from that user until the timer runs out, any other chat messages are ignored
    - if the timer runs out, we move on to the next player
- if the player responds with more than one letter, we will assume they're guessing the whole solution or using a !pass command to skip their turn
    - if they guess correctly
        - they win the round and are given the number of points they accumulated in that round
        - if they had the 'gift sub' space, ian will award them a gift sub
- if they only send one letter
    - if the letter was previously guessed, the player loses their turn
    - if the letter is not in the solution, the player loses their turn
    - if they guess a letter in the solution, we will reveal every occurence of that letter in the solution
        - the game will spin the wheel again and start another timer
        - this player continues their turn until their timer runs out or they lose their turn
- each successive game will double the point values on the wheel except the special places


### word ladder

- starting the game will look like "!game word-ladder 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin
    - players can !join to play that round until that 60 second timer ends, then no more players can join
- the bot will randomize the order of the players when the round starts

- the bot will randomize the order of the players when the round starts

- streamerbot chooses a random number between 4 and 6, and chooses two words from the english dictionary each with that number of letters; these are the "solution words"
    - the solution words should be related in some way, examples:
        - they are both colors, or foods, or have a similar subject (things in a classroom, things in a kitchen, etc)
        - they are related to a sport (ie, "puck" and "goal" for hockey)
        -
- each ladder step must only change one letter in each word, and also be a valid word in the english dictionary, to change from one solution word to the other
- there should be 4-6 steps between the two words, so a total of 6 to 8 words will be needed per round
- the two solution words will be at top and bottom of the ladder; if the solution words are sorted alphabetically the 'lower' word will be at the top of the latter, the 'higher' word will be at the bottom

- the game will show a timer close for each player's turn
- the game board will show rows of tiles, one row per word, and one tile per letter in the words
- the first two words are not revealed but the background behind those two words will be a different color

- the game will start at the top of the ladder (except for the solution word, so in a list of words the game would start at index position 1, not 0)
    - the game should draw an arrow pointing at the word to guess
- the game will show a clue about that word and start a 15-second timer and mention the next player to try a guess
- the game will attend to that player's messages, all others will be ignored
    - the game will allow uppercase or lowercase letters
    - if the player guesses the correct word
        - it will be revealed on the game board
        - they are awarded 100 points
    - if they guess incorrectly or their timer runs out, they lose their turn

- once all non-solution words are revealed
    - the game will reveal the clue about the solution words
    - all players can send chat messages guessing the two words like "word1 word2" or "word2 word1"
    - the first player who sends a chat message that correctly guesses the two words will win 1000 addiitonal points





### battleship

- starting the game will look like "!game battleship 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin
    - players can !join to play that round until that 60 second timer ends, then no more players can join

- game will show a grid of squares starting at a 10x10 grid, drawn as a grid where columns are A-J and rows are 1-10
    - each successive game will increase the grid by 5 spaces (10x10 -> 15x15 -> 20x20 and so on)
- game will show the remaining ships on the board plus number of mines
- game will hide a series of ships in those squares
- ships can only be placed horizontally or vertically, never diagonally, and cannot go outside of the grid coordinates like wrapping around from one side to the other or from the top to bottom
- the ships placed will have different sizes:
    - one ship is 5 squares long
    - one ship is 4 squares long
    - two ships are 3 squares long
    - one ship is 2 squares long
- the game will also hide 4 "mines" on the board which take up one square

- a timer will run for 15 seconds per round
    - players who joined can type in a coordinate like 'A12' for column A and row 12, or 'A 12' (the game will allow uppercase or lowercase letters)
    - all choices typed in by players will be "averaged", equating letters to their 1-25 counterpart (A=1, B=2 etc)
        - averages will be rounded as integers
        - if two players enter "A1" and "I19" then the average of A through I will be A=1 and I=9 so the average will be 5 -> E, and the numbers of their coordinates of 1 and 19 will average to 20/2 = 10, so the 'average' coordinate will equal 'E 10'
    - the final coordinate, based on the average, will be fired upon
        - if that coordinate does not have a ship hidden in that square, replace the square with a white peg
        - if a ship was hidden in that space, replace that square with a red peg
        - if a mine was hidden in the space, the game is over
    - if the final square for a ship is hit, that ship is "sunk" and removed from the board and the ship image on the side of the board will ahve a red X through it
- the game ends if a mine is hit by the players or all ships are fully sunk
- if the ships were sunk, all players win 1000 points; no penalty if a mine was hit
- each additional game will increase the grid by 5 rows and columns, and double the previous number of mines (4 to 8 to 16 etc)



### tic-tac-toe

- starting the game will look like "!game tictactoe 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin
    - players can !join to play that round until that 60 second timer ends, then no more players can join

- only up to the first 12 players who !join will play
- players will be paired up randomly into a mini-game
    - each pair will be assigned X or O randomly and told in chat like "match-up: player1 is X, player2 is O" and will send one message for each pair

- game will draw one tic-tac-toe board per mini-game (pair of players) and will have 3 rows and columns labeled A B or C for columns and 1 2 and 3 for rows
- players will enter a coordinate like "a1" as a letter/number combination for their choice of column/row to place their X or O
    - players can only place in an empty place
- if a player gets all 3 of their tokens in a row (horizontal, vertical or diagonal) they win their mini-game

- the player who wins their mini-game will win 100 points and move onto a second round to play one of the winners of a different mini-game; the other player is eliminated
    - if there were an odd number of mini-games in the previous round, one player will wait until the following round
- the second round winners will win 200 points and those winners will be paired up for another round
- when only one mini-game winner remains that player wins 1000 points



### simon says

- chat game only, no on-screen visuals
- players can !join

- a timer will start at 60 seconds
- when the timer begins, the bot will send a message that says "Simon Says: emote"
    - players who join have 15 seconds to respond with a message that only contains that emote
        - other messages from that player will be ignored
    - if a player doesn't respond with a message with just that matching emote, they are eliminated from the game
    - players who send the matching emote will will win 100 points
    - when the timer is done, it will reset at 10 seconds less than last time (60 -> 50 -> 40 etc)
- any player at the end of the game who successfully sent the correct emote at each stage will win an aditional 400 points




## other game ideas

### (done) stock market

- this could run all the time in chat
- streamerbot will assign a current 'stock price' to one of 10 emotes, and will print like "emote1: $price ... emote2: $price ... emote3: $price ..."
- game will send stock prices to chat every 10 minutes while stream is running
- players can use their current points to 'buy' an emote with '!buy emotename quantity'
    - game will check if player has that many points (ten points is $1)
        - if they have enough points, points are reduced to buy that emote and store that quantity in their 'portfolio'
- players can use "!sell emotename quantity" to sell their emote stocks (if they own that quantity), to gain back that number of points given the new price
- players cannot go lower than 0 points
- players can use a !stocks command to see their portfolio



### minesweeper

- this would be an interesting one to build
- the first 6 players who !join get to play
- they get a 5x5 grid
- they type in coordinates A through E and 1 through 5 for columns and rows
- "!flag a1" will set a flag at that coordinate on their board
- "!show b3" will reveal that coordinate per typical minesweeper rules
- if a mine is revealed, that player's game is over