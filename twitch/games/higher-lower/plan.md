### higher or lower

- starting the game will look like "!game higher-lower 3" meaning we'll play 3 games, each round will have a 60-second countdown to begin; only mods or the streamer can run this command
    - players can !join to play that round until that 60 second timer ends, then no more players can join
    - the game will need to track a timer and announce in chat every 10 seconds how long players have to join until the game starts
    - only players who !join will have their chat messages watched by the game

- game will pick a random number from 1 to 100, and set the winning points to 1000 points
- the game will have 10 rounds; in each round:
    - for the next 30 seconds, players who did a !join command can enter a number in chat, if they enter more than one number, only the first number they typed in will count
    - after 30 seconds have ended:
        - all numbers entered by users in that round will be averaged and rounded as an integer and submitted as a guess against the game-chosen number
        - the game will show a message at the bottom of the board that reads "guess higher" or "guess lower" if the number is not correct, and reduce the winnable points by 100 points
        - if the average guess is the correct answer, each player will win a percentage of the winnable points based on how many rounds they particpated. if the game lasts 5 rounds I particpated in 1 round, I win 20% of the winnable points, if I participated in all 5 rounds i win 100% of the winnable points
- if the game runs out of the 10 rounds without a correct averaged guess from the players, nobody will win any points
- if any player during the game guessed the correct answer, they will win an additional 1000 points


plan:
- build a new group called 'higher-lower-group'
- when !game higher-lower is activated
    - show OBS '60-second !join' source
    - get timestampNow + 60 seconds = SetGlobal "join-cutoff"
    - watch chat for !join messages until "join_cutoff" is met
        - if not over, add user to 'higher-lower-group'
        - if over, replay with "sorry, game is already started"
    - hide OBS '60-second !join' source

    - guess random number 1-100
    - set 'winnable-points' to 1000
    - create set of strings for 'exact-guessers'
    - create dictionary for 'guessers'
    - set number-guessed to False

    - show OBS '15-second until round starts' source
    - wait 15 seconds
    - hide OBS '15-second until round starts' source

    - set round-number to 1
LOOP while round-number <= 10 or !number-guessed:
    - create a set 'round-{round-numer}-guessers'
    - create list 'round-{round-number}-guesses'

    - setglobal "number-cutoff" for now + 30 seconds
    - show OBS '30-second guess a number' source
    - enable action for 'watch chat messages'
    - wait 30 seconds
    - disable action for 'watch chat messages'

    - watch chat messages:
        - if user not in group: do nothing
        - if trimmed chat message is only a positive number
            - if user not in 'round-N-guesses' set
                - add user to set 'round-1'
                - add number to 'round-N-guesses'
                - add username:(increment) to 'guessers' dictionary
            - if number exactly matches random choice
                - add user to 'exact-guessers' list

    - hide OBS '30-second guess a number' source

    - calc average of guesses, rounded to nearest int
        - if number matches
            - set number-guessed to True
            - award everyone in group the 'winnable' number of points:
                - for each player, calculate percentage of points based on participation/rounds
                    - if rounds=5 and participation count was 4, they get 4/5's of winnable points
            - break out of loop
        - else
            - reduce winnable points by 100
            - if averaged number is lower than random choice, tell chat "guess higher"
            - else, tell chat "guess lower"
            - tell chat how many points are left to win
            - increase 'round' by 1


- when game is over
    - aware anyone in exact-guessers 1000 points
    - erase all 'round-N-guesses' sets
    - erase all 'round-N-guessers' sets
    - erase 'exact-guessers'
    - remove everyone from 'higher-lower-group'
